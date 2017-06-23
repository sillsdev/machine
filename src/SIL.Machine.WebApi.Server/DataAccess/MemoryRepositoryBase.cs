using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Server.Models;
using SIL.Threading;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public class MemoryRepositoryBase<T> : IRepository<T> where T : class, IEntity<T>
	{
		private readonly Dictionary<string, Action<EntityChange<T>>> _changeListeners;

		protected MemoryRepositoryBase(IRepository<T> persistenceRepo = null)
		{
			Lock = new AsyncReaderWriterLock();
			Entities = new Dictionary<string, T>();
			PersistenceRepository = persistenceRepo;
			_changeListeners = new Dictionary<string, Action<EntityChange<T>>>();
			if (PersistenceRepository != null)
			{
				foreach (T entity in PersistenceRepository.GetAll())
					Entities[entity.Id] = entity;
			}
		}

		protected IRepository<T> PersistenceRepository { get; }
		protected AsyncReaderWriterLock Lock { get; }
		protected IDictionary<string, T> Entities { get; }

		public IEnumerable<T> GetAll()
		{
			using (Lock.ReaderLock())
				return GetAllEntities();
		}

		public bool TryGet(string id, out T entity)
		{
			using (Lock.ReaderLock())
				return TryGetEntity(id, out entity);
		}

		public void Insert(T entity)
		{
			var changeListeners = new List<Action<EntityChange<T>>>();
			T internalEntity;
			using (Lock.WriterLock())
			{
				internalEntity = InsertEntity(entity, changeListeners);
				PersistenceRepository?.Insert(entity);
			}
			SendToSubscribers(changeListeners, EntityChangeType.Insert, internalEntity);
		}

		public void Update(T entity, bool checkConflict = false)
		{
			var changeListeners = new List<Action<EntityChange<T>>>();
			T internalEntity;
			using (Lock.WriterLock())
			{
				internalEntity = UpdateEntity(entity, changeListeners, checkConflict);
				PersistenceRepository?.Update(entity);
			}
			SendToSubscribers(changeListeners, EntityChangeType.Update, internalEntity);
		}

		public void Delete(T entity, bool checkConflict = false)
		{
			var changeListeners = new List<Action<EntityChange<T>>>();
			T internalEntity;
			using (Lock.WriterLock())
			{
				internalEntity = DeleteEntity(entity, changeListeners, checkConflict);
				PersistenceRepository?.Delete(entity);
			}
			SendToSubscribers(changeListeners, EntityChangeType.Delete, internalEntity);
		}

		public void Delete(string id)
		{
			var changeListeners = new List<Action<EntityChange<T>>>();
			T internalEntity;
			using (Lock.WriterLock())
			{
				internalEntity = DeleteEntity(id, changeListeners);
				PersistenceRepository?.Delete(id);
			}
			SendToSubscribers(changeListeners, EntityChangeType.Delete, internalEntity);
		}

		public async Task<IEnumerable<T>> GetAllAsync()
		{
			using (await Lock.ReaderLockAsync())
				return GetAllEntities();
		}

		public async Task<T> GetAsync(string id)
		{
			using (await Lock.ReaderLockAsync())
			{
				if (TryGetEntity(id, out T entity))
					return entity;
				return null;
			}
		}

		public async Task InsertAsync(T entity)
		{
			var changeListeners = new List<Action<EntityChange<T>>>();
			T internalEntity;
			using (await Lock.WriterLockAsync())
			{
				internalEntity = InsertEntity(entity, changeListeners);
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
				internalEntity = UpdateEntity(entity, changeListeners, checkConflict);
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
				internalEntity = DeleteEntity(entity, changeListeners, checkConflict);
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
			SendToSubscribers(changeListeners, EntityChangeType.Delete, internalEntity);
		}

		public async Task<IDisposable> SubscribeAsync(string id, Action<EntityChange<T>> listener)
		{
			using (await Lock.WriterLockAsync())
			{
				_changeListeners[id] = listener;
				return new Subscription<string, T>(Lock, _changeListeners, id);
			}
		}

		private IEnumerable<T> GetAllEntities()
		{
			return Entities.Values.Select(e => e.Clone()).ToArray();
		}

		private bool TryGetEntity(string id, out T entity)
		{
			if (Entities.TryGetValue(id, out T e))
			{
				entity = e.Clone();
				return true;
			}

			entity = null;
			return false;
		}

		private T InsertEntity(T entity, IList<Action<EntityChange<T>>> changeListeners)
		{
			if (string.IsNullOrEmpty(entity.Id))
				entity.Id = ObjectId.GenerateNewId().ToString();
			if (Entities.TryGetValue(entity.Id, out T otherEntity))
			{
				throw new KeyAlreadyExistsException("An entity with the same identifier already exists.")
				{
					Entity = otherEntity
				};
			}
			OnBeforeEntityChanged(EntityChangeType.Insert, entity);

			T internalEntity = entity.Clone();
			Entities.Add(entity.Id, internalEntity);

			OnEntityChanged(EntityChangeType.Insert, internalEntity, changeListeners);

			return internalEntity;
		}

		private T UpdateEntity(T entity, IList<Action<EntityChange<T>>> changeListeners, bool checkConflict)
		{
			OnBeforeEntityChanged(EntityChangeType.Update, entity);

			if (checkConflict)
				CheckForConcurrencyConflict(entity);

			entity.Revision++;
			T internalEntity = entity.Clone();
			Entities[entity.Id] = internalEntity;

			if (_changeListeners.TryGetValue(entity.Id, out Action<EntityChange<T>> changeListener))
				changeListeners.Add(changeListener);

			OnEntityChanged(EntityChangeType.Update, internalEntity, changeListeners);

			return internalEntity;
		}

		private T DeleteEntity(T entity, IList<Action<EntityChange<T>>> changeListeners, bool checkConflict)
		{
			if (checkConflict)
				CheckForConcurrencyConflict(entity);

			return DeleteEntity(entity.Id, changeListeners);
		}

		private T DeleteEntity(string id, IList<Action<EntityChange<T>>> changeListeners)
		{
			if (Entities.TryGetValue(id, out T internalEntity))
			{
				Entities.Remove(id);

				if (_changeListeners.TryGetValue(id, out Action<EntityChange<T>> changeListener))
					changeListeners.Add(changeListener);

				OnEntityChanged(EntityChangeType.Delete, internalEntity, changeListeners);
			}
			return internalEntity;
		}

		private void CheckForConcurrencyConflict(T entity)
		{
			if (!Entities.TryGetValue(entity.Id, out T internalEntity))
			{
				throw new ConcurrencyConflictException("The entity has been deleted.")
				{
					ExpectedRevision = entity.Revision
				};
			}

			if (entity.Revision != internalEntity.Revision)
			{
				throw new ConcurrencyConflictException("The entity has been updated.")
				{
					ExpectedRevision = entity.Revision,
					ActualRevision = internalEntity.Revision
				};
			}
		}

		private void SendToSubscribers(IList<Action<EntityChange<T>>> changeListeners, EntityChangeType type, T entity)
		{
			foreach (Action<EntityChange<T>> changeListener in changeListeners)
				changeListener(new EntityChange<T>(type, entity.Clone()));
		}

		protected virtual void OnBeforeEntityChanged(EntityChangeType type, T entity)
		{
		}

		protected virtual void OnEntityChanged(EntityChangeType type, T internalEntity,
			IList<Action<EntityChange<T>>> changeListeners)
		{
		}
	}
}
