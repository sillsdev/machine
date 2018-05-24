using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Utils;

namespace SIL.Machine.WebApi.Server.DataAccess.Memory
{
	internal class UniqueEntityIndex<TKey, TEntity> where TEntity : class, IEntity<TEntity>
	{
		private readonly Dictionary<TKey, TEntity> _index;
		private readonly Func<TEntity, IEnumerable<TKey>> _keySelector;
		private readonly Func<TEntity, bool> _filter;
		private readonly Dictionary<TKey, ISet<Action<EntityChange<TEntity>>>> _changeListeners;

		public UniqueEntityIndex(Func<TEntity, TKey> keySelector, Func<TEntity, bool> filter = null)
			: this(e => keySelector(e).ToEnumerable(), filter)
		{
		}

		public UniqueEntityIndex(Func<TEntity, IEnumerable<TKey>> keySelector, Func<TEntity, bool> filter = null)
		{
			_index = new Dictionary<TKey, TEntity>();
			_keySelector = keySelector;
			_filter = filter;
			_changeListeners = new Dictionary<TKey, ISet<Action<EntityChange<TEntity>>>>();
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

		public void OnEntityUpdated(TEntity oldEntity, TEntity newEntity,
			IList<Action<EntityChange<TEntity>>> changeListeners)
		{
			if (_filter != null && !_filter(newEntity))
				return;

			var keysToRemove = new HashSet<TKey>(oldEntity == null ? Enumerable.Empty<TKey>()
				: _keySelector(oldEntity));
			foreach (TKey key in _keySelector(newEntity))
			{
				_index[key] = newEntity;
				keysToRemove.Remove(key);
				if (changeListeners != null)
				{
					if (_changeListeners.TryGetValue(key, out ISet<Action<EntityChange<TEntity>>> listeners))
						changeListeners.AddRange(listeners);
				}
			}

			foreach (TKey key in keysToRemove)
				_index.Remove(key);
		}

		public void OnEntityDeleted(TEntity entity, IList<Action<EntityChange<TEntity>>> changeListeners)
		{
			if (_filter != null && !_filter(entity))
				return;

			foreach (TKey key in _keySelector(entity))
			{
				_index.Remove(key);

				if (changeListeners != null)
				{
					if (_changeListeners.TryGetValue(key, out ISet<Action<EntityChange<TEntity>>> listeners))
						changeListeners.AddRange(listeners);
				}
			}
		}

		public IDisposable Subscribe(AsyncReaderWriterLock repoLock, TKey key, Action<EntityChange<TEntity>> listener)
		{
			return new MemorySubscription<TKey, TEntity>(repoLock, _changeListeners, key, listener);
		}
	}
}

