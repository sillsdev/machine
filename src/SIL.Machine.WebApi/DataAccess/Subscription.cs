using System;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Threading;
using SIL.Machine.WebApi.Models;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.DataAccess
{
	public class Subscription<T> : DisposableBase where T : class, IEntity<T>
	{
		private readonly Action<Subscription<T>> _remove;
		private readonly AsyncAutoResetEvent _changeEvent;

		public Subscription(object key, T initialEntity, Action<Subscription<T>> remove)
		{
			Key = key;
			_remove = remove;
			_changeEvent = new AsyncAutoResetEvent();
			Change = new EntityChange<T>(initialEntity == null ? EntityChangeType.Delete : EntityChangeType.Update,
				initialEntity);
		}

		public EntityChange<T> Change { get; private set; }

		internal object Key { get; }

		public Task WaitForUpdateAsync(CancellationToken ct = default(CancellationToken))
		{
			return _changeEvent.WaitAsync(ct);
		}

		internal void HandleChange(EntityChange<T> change)
		{
			Change = change;
			_changeEvent.Set();
		}

		protected override void DisposeManagedResources()
		{
			_remove(this);
		}
	}
}