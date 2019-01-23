using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Utils
{
	public class ObjectPool<T> : DisposableBase
	{
		private readonly BufferBlock<T> _bufferBlock;
		private readonly Func<Task<T>> _factory;
		private readonly AsyncLock _lock;
		private readonly List<T> _objs;

		public ObjectPool(int maxCount, Func<Task<T>> factory)
		{
			_lock = new AsyncLock();
			MaxCount = maxCount;
			_factory = factory;
			_bufferBlock = new BufferBlock<T>();
			_objs = new List<T>();
		}

		public int MaxCount { get; }
		public int Count { get; private set; }
		public int AvailableCount => _bufferBlock.Count;

		public async Task<ObjectPoolItem<T>> GetAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			CheckDisposed();

			if (_bufferBlock.TryReceive(out T obj))
				return new ObjectPoolItem<T>(this, obj);

			if (Count < MaxCount)
			{
				using (await _lock.LockAsync(cancellationToken))
				{
					if (Count < MaxCount)
					{
						Count++;
						obj = await _factory();
						_objs.Add(obj);
						_bufferBlock.Post(obj);
					}
				}
			}

			return new ObjectPoolItem<T>(this, await _bufferBlock.ReceiveAsync(cancellationToken));
		}

		internal void Put(T item)
		{
			CheckDisposed();

			_bufferBlock.Post(item);
		}

		protected override void DisposeManagedResources()
		{
			_bufferBlock.TryReceiveAll(out _);
			foreach (T obj in _objs)
			{
				var disposable = obj as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			_objs.Clear();
			Count = 0;
			_bufferBlock.Complete();
		}
	}
}
