using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SIL.Extensions;
using SIL.Machine.Threading;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.Memory
{
	internal class UniqueEntityIndex<TKey, TEntity> where TEntity : class, IEntity<TEntity>
	{
		private readonly AsyncReaderWriterLock _repoLock;
		private readonly Dictionary<TKey, TEntity> _index;
		private readonly Func<TEntity, IEnumerable<TKey>> _keySelector;
		private readonly Func<TEntity, bool> _filter;
		private readonly Dictionary<TKey, ISet<Subscription<TEntity>>> _keySubscriptions;

		public UniqueEntityIndex(AsyncReaderWriterLock repoLock, Func<TEntity, TKey> keySelector,
			Func<TEntity, bool> filter = null) : this(repoLock, e => keySelector(e).ToEnumerable(), filter)
		{
		}

		public UniqueEntityIndex(AsyncReaderWriterLock repoLock, Func<TEntity, IEnumerable<TKey>> keySelector,
			Func<TEntity, bool> filter = null)
		{
			_repoLock = repoLock;
			_index = new Dictionary<TKey, TEntity>();
			_keySelector = keySelector;
			_filter = filter;
			_keySubscriptions = new Dictionary<TKey, ISet<Subscription<TEntity>>>();
		}

		public bool TryGetEntity(TKey key, out TEntity entity)
		{
			if (_index.TryGetValue(key, out TEntity e))
			{
				entity = e.Clone();
				return true;
			}

			entity = null;
			return false;
		}

		public void CheckKeyConflict(TEntity entity)
		{
			if (_filter != null && !_filter(entity))
				return;

			foreach (TKey key in _keySelector(entity))
			{
				if (_index.TryGetValue(key, out TEntity otherEntity))
				{
					if (entity.Id != otherEntity.Id)
						throw new KeyAlreadyExistsException("An entity with the same key already exists.");
				}
			}
		}

		public void PopulateIndex(IEnumerable<TEntity> entities)
		{
			foreach (TEntity entity in entities)
				OnEntityUpdated(null, entity, null);
		}

		public void OnEntityUpdated(TEntity oldEntity, TEntity newEntity, IList<Subscription<TEntity>> allSubscriptions)
		{
			if (_filter != null && !_filter(newEntity))
				return;

			var keysToRemove = new HashSet<TKey>(oldEntity == null ? Enumerable.Empty<TKey>()
				: _keySelector(oldEntity));
			foreach (TKey key in _keySelector(newEntity))
			{
				_index[key] = newEntity;
				keysToRemove.Remove(key);
				if (allSubscriptions != null)
					GetKeySubscriptions(key, allSubscriptions);
			}

			foreach (TKey key in keysToRemove)
				_index.Remove(key);
		}

		public void OnEntityDeleted(TEntity entity, IList<Subscription<TEntity>> allSubscriptions)
		{
			if (_filter != null && !_filter(entity))
				return;

			foreach (TKey key in _keySelector(entity))
			{
				_index.Remove(key);

				if (allSubscriptions != null)
					GetKeySubscriptions(key, allSubscriptions);
			}
		}

		public async Task<Subscription<TEntity>> SubscribeAsync(TKey key,
			CancellationToken ct = default(CancellationToken))
		{
			using (await _repoLock.WriterLockAsync(ct))
			{
				TryGetEntity(key, out TEntity initialEntity);
				var subscription = new Subscription<TEntity>(key, initialEntity, RemoveSubscription);
				if (!_keySubscriptions.TryGetValue(key, out ISet<Subscription<TEntity>> subscriptions))
				{
					subscriptions = new HashSet<Subscription<TEntity>>();
					_keySubscriptions[key] = subscriptions;
				}
				subscriptions.Add(subscription);
				return subscription;
			}
		}

		private void RemoveSubscription(Subscription<TEntity> subscription)
		{
			using (_repoLock.WriterLock())
			{
				var key = (TKey) subscription.Key;
				ISet<Subscription<TEntity>> subscriptions = _keySubscriptions[key];
				subscriptions.Remove(subscription);
				if (subscriptions.Count == 0)
					_keySubscriptions.Remove(key);
			}
		}

		private void GetKeySubscriptions(TKey key, IList<Subscription<TEntity>> allSubscriptions)
		{
			if (_keySubscriptions.TryGetValue(key, out ISet<Subscription<TEntity>> subscriptions))
				allSubscriptions.AddRange(subscriptions);
		}
	}
}

