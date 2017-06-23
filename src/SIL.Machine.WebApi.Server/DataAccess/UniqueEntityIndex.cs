using System;
using System.Collections.Generic;
using SIL.Extensions;
using SIL.Machine.WebApi.Server.Models;
using SIL.Threading;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	internal class UniqueEntityIndex<TKey, TEntity> where TEntity : class, IEntity<TEntity>
	{
		private readonly Dictionary<TKey, TEntity> _index;
		private readonly Func<TEntity, IEnumerable<TKey>> _keySelector;
		private readonly Func<TEntity, bool> _filter;
		private readonly Dictionary<TKey, Action<EntityChange<TEntity>>> _changeListeners;

		public UniqueEntityIndex(Func<TEntity, TKey> keySelector, Func<TEntity, bool> filter = null)
			: this(e => keySelector(e).ToEnumerable(), filter)
		{
		}

		public UniqueEntityIndex(Func<TEntity, IEnumerable<TKey>> keySelector, Func<TEntity, bool> filter = null)
		{
			_index = new Dictionary<TKey, TEntity>();
			_keySelector = keySelector;
			_filter = filter;
			_changeListeners = new Dictionary<TKey, Action<EntityChange<TEntity>>>();
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
					{
						throw new KeyAlreadyExistsException("An entity with the same key already exists.")
						{
							Entity = otherEntity
						};
					}
				}
			}
		}

		public void PopulateIndex(IEnumerable<TEntity> entities)
		{
			foreach (TEntity entity in entities)
				OnEntityUpdated(entity, null);
		}

		public void OnEntityUpdated(TEntity entity, IList<Action<EntityChange<TEntity>>> changeListeners)
		{
			if (_filter != null && !_filter(entity))
				return;

			foreach (TKey key in _keySelector(entity))
			{
				_index[key] = entity;

				if (changeListeners != null)
				{
					if (_changeListeners.TryGetValue(key, out Action<EntityChange<TEntity>> changeListener))
						changeListeners.Add(changeListener);
				}
			}
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
					if (_changeListeners.TryGetValue(key, out Action<EntityChange<TEntity>> changeListener))
						changeListeners.Add(changeListener);
				}
			}
		}

		public IDisposable Subscribe(AsyncReaderWriterLock repoLock, TKey key, Action<EntityChange<TEntity>> listener)
		{
			_changeListeners[key] = listener;
			return new Subscription<TKey, TEntity>(repoLock, _changeListeners, key);
		}
	}
}

