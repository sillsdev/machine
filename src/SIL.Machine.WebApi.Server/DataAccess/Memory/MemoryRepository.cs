using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using SIL.Extensions;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Utils;

namespace SIL.Machine.WebApi.Server.DataAccess.Memory
{
	public class MemoryRepository<T> : IRepository<T> where T : class, IEntity<T>
	{
		private readonly Dictionary<string, ISet<Action<EntityChange<T>>>> _changeListeners;

		public MemoryRepository(IRepository<T> persistenceRepo = null)
		{
			Lock = new AsyncReaderWriterLock();
			Entities = new Dictionary<string, T>();
			PersistenceRepository = persistenceRepo;
			_changeListeners = new Dictionary<string, ISet<Action<EntityChange<T>>>>();
		}

		protected IRepository<T> PersistenceRepository { get; }
		protected AsyncReaderWriterLock Lock { get; }
		protected IDictionary<string, T> Entities { get; }

		public virtual async Task InitAsync()
		{
			if (PersistenceRepository != null)
			{
				await PersistenceRepository.InitAsync();
				foreach (T entity in await PersistenceRepository.GetAllAsync())
					Entities[entity.Id] = entity;
			}
		}

		public async Task<IEnumerable<T>> GetAllAsync()
		{
			using (await Lock.ReaderLockAsync())
				return Entities.Values.Select(e => e.Clone()).ToArray();
		}

		public async Task<T> GetAsync(string id)
		{
			using (await Lock.ReaderLockAsync())
			{
				if (Entities.TryGetValue(id, out T e))
					return e.Clone();

				return null;
			}
		}

		public async Task InsertAsync(T entity)
		{
			var changeListeners = new List<Action<EntityChange<T>>>();
			T internalEntity;
			using (await Lock.WriterLockAsync())
			{
				if (string.IsNullOrEmpty(entity.Id))
					entity.Id = ObjectId.GenerateNewId().ToString();
				if (Entities.ContainsKey(entity.Id))
					throw new KeyAlreadyExistsException("An entity with the same identifier already exists.");
				OnBeforeEntityChanged(EntityChangeType.Insert, entity);

				internalEntity = entity.Clone();
				Entities.Add(entity.Id, internalEntity);

				OnEntityChanged(EntityChangeType.Insert, null, internalEntity, changeListeners);

				if (PersistenceRepository != null)
					await PersistenceRepository.InsertAsync(entity);
			}
			SendToSubscribers(changeListeners, EntityChangeType.Insert, internalEntity);
		}

		public async Task UpdateAsync(T entity, bool checkConflict = false)
		{
			var changeListeners = new List<Action<EntityChange<T>>>();
			T internalEntity;
			using (await Lock.WriterLockAsync())
			{
				OnBeforeEntityChanged(EntityChangeType.Update, entity);

				if (checkConflict)
					CheckForConcurrencyConflict(entity);

				entity.Revision++;
				internalEntity = entity.Clone();
				T oldEntity = Entities[entity.Id];
				Entities[entity.Id] = internalEntity;

				if (_changeListeners.TryGetValue(entity.Id, out ISet<Action<EntityChange<T>>> listeners))
					changeListeners.AddRange(listeners);

				OnEntityChanged(EntityChangeType.Update, oldEntity, internalEntity, changeListeners);

				if (PersistenceRepository != null)
					await PersistenceRepository.UpdateAsync(entity);
			}
			SendToSubscribers(changeListeners, EntityChangeType.Update, internalEntity);
		}

		public async Task DeleteAsync(T entity, bool checkConflict = false)
		{
			var changeListeners = new List<Action<EntityChange<T>>>();
			T internalEntity;
			using (await Lock.WriterLockAsync())
			{
				if (checkConflict)
					CheckForConcurrencyConflict(entity);

				internalEntity = DeleteEntity(entity.Id, changeListeners);

				if (PersistenceRepository != null)
					await PersistenceRepository.DeleteAsync(entity);
			}
			SendToSubscribers(changeListeners, EntityChangeType.Delete, internalEntity);
		}

		public async Task DeleteAsync(string id)
		{
			var changeListeners = new List<Action<EntityChange<T>>>();
			T internalEntity;
			using (await Lock.WriterLockAsync())
			{
				internalEntity = DeleteEntity(id, changeListeners);

				if (PersistenceRepository != null)
					await PersistenceRepository.DeleteAsync(id);
			}
			if (internalEntity != null)
				SendToSubscribers(changeListeners, EntityChangeType.Delete, internalEntity);
		}

		public async Task<IDisposable> SubscribeAsync(string id, Action<EntityChange<T>> listener)
		{
			using (await Lock.WriterLockAsync())
			{
				return new MemorySubscription<string, T>(Lock, _changeListeners, id, listener);
			}
		}

		private T DeleteEntity(string id, IList<Action<EntityChange<T>>> changeListeners)
		{
			if (Entities.TryGetValue(id, out T oldEntity))
			{
				Entities.Remove(id);

				if (_changeListeners.TryGetValue(id, out ISet<Action<EntityChange<T>>> listeners))
					changeListeners.AddRange(listeners);

				OnEntityChanged(EntityChangeType.Delete, oldEntity, null, changeListeners);
				return oldEntity;
			}
			return null;
		}

		private void CheckForConcurrencyConflict(T entity)
		{
			if (!Entities.TryGetValue(entity.Id, out T internalEntity))
				throw new ConcurrencyConflictException("The entity does not exist.");

			if (entity.Revision != internalEntity.Revision)
				throw new ConcurrencyConflictException("The entity has been updated.");
		}

		private void SendToSubscribers(IList<Action<EntityChange<T>>> changeListeners, EntityChangeType type, T entity)
		{
			foreach (Action<EntityChange<T>> changeListener in changeListeners)
				changeListener(new EntityChange<T>(type, entity.Clone()));
		}

		protected virtual void OnBeforeEntityChanged(EntityChangeType type, T entity)
		{
		}

		protected virtual void OnEntityChanged(EntityChangeType type, T oldEntity, T newEntity,
			IList<Action<EntityChange<T>>> changeListeners)
		{
		}
	}
}
