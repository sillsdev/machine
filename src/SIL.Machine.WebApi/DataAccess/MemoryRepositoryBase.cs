using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;
using SIL.ObjectModel;
using SIL.Threading;

namespace SIL.Machine.WebApi.DataAccess
{
	public class MemoryRepositoryBase<T> : IRepository<T> where T : class, IEntity<T>
	{
		private readonly Dictionary<string, Action<T>> _changeListeners;

		protected MemoryRepositoryBase(IRepository<T> persistenceRepo = null)
		{
			Lock = new AsyncReaderWriterLock();
			Entities = new Dictionary<string, T>();
			Indices = new KeyedList<string, UniqueEntityIndex<T>>(i => i.Name);
			PersistenceRepository = persistenceRepo;
			_changeListeners = new Dictionary<string, Action<T>>();
			if (PersistenceRepository != null)
			{
				foreach (T entity in PersistenceRepository.GetAll())
					Entities[entity.Id] = entity;
			}
		}

		protected IRepository<T> PersistenceRepository { get; }
		protected AsyncReaderWriterLock Lock { get; }
		protected IDictionary<string, T> Entities { get; }
		protected IKeyedCollection<string, UniqueEntityIndex<T>> Indices { get; }

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
			using (Lock.WriterLock())
			{
				InsertEntity(entity);
				PersistenceRepository?.Insert(entity);
			}
		}

		public void Update(T entity, bool checkConflict = false)
		{
			Action<T> changeListener;
			using (Lock.WriterLock())
			{
				UpdateEntity(entity, checkConflict);
				PersistenceRepository?.Update(entity);
				_changeListeners.TryGetValue(entity.Id, out changeListener);
			}
			changeListener?.Invoke(entity.Clone());
		}

		public void Delete(T entity, bool checkConflict = false)
		{
			Action<T> changeListener;
			using (Lock.WriterLock())
			{
				DeleteEntity(entity, checkConflict);
				PersistenceRepository?.Delete(entity);
				_changeListeners.TryGetValue(entity.Id, out changeListener);
			}
			changeListener?.Invoke(null);
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
			using (await Lock.WriterLockAsync())
			{
				InsertEntity(entity);
				if (PersistenceRepository != null)
					await PersistenceRepository.InsertAsync(entity);
			}
		}

		public async Task UpdateAsync(T entity, bool checkConflict = false)
		{
			Action<T> changeListener;
			using (await Lock.WriterLockAsync())
			{
				UpdateEntity(entity, checkConflict);
				if (PersistenceRepository != null)
					await PersistenceRepository.UpdateAsync(entity);
				_changeListeners.TryGetValue(entity.Id, out changeListener);
			}
			changeListener?.Invoke(entity.Clone());
		}

		public async Task DeleteAsync(T entity, bool checkConflict = false)
		{
			Action<T> changeListener;
			using (await Lock.WriterLockAsync())
			{
				DeleteEntity(entity, checkConflict);
				if (PersistenceRepository != null)
					await PersistenceRepository.DeleteAsync(entity);
				_changeListeners.TryGetValue(entity.Id, out changeListener);
			}
			changeListener?.Invoke(null);
		}

		public async Task<IDisposable> SubscribeAsync(string id, Action<T> listener)
		{
			using (await Lock.WriterLockAsync())
			{
				_changeListeners[id] = listener;
				return new Subscription(this, id);
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

		private void InsertEntity(T entity)
		{
			if (string.IsNullOrEmpty(entity.Id))
				entity.Id = ObjectId.GenerateNewId().ToString();
			if (Entities.TryGetValue(entity.Id, out T otherEntity))
			{
				throw new KeyAlreadyExistsException("An entity with the same identifier already exists.")
				{
					IndexName = "Id",
					Entity = otherEntity
				};
			}
			foreach (UniqueEntityIndex<T> index in Indices)
				index.CheckKeyConflict(entity);

			T internalEntity = entity.Clone();
			Entities.Add(entity.Id, internalEntity);

			foreach (UniqueEntityIndex<T> index in Indices)
				index.EntityUpdated(internalEntity);
		}

		private void UpdateEntity(T entity, bool checkConflict)
		{
			foreach (UniqueEntityIndex<T> index in Indices)
				index.CheckKeyConflict(entity);
			if (checkConflict)
				CheckForConcurrencyConflict(entity);

			entity.Revision++;
			T internalEntity = entity.Clone();
			Entities[entity.Id] = internalEntity;

			foreach (UniqueEntityIndex<T> index in Indices)
				index.EntityUpdated(internalEntity);
		}

		private void DeleteEntity(T entity, bool checkConflict)
		{
			if (checkConflict)
				CheckForConcurrencyConflict(entity);

			if (Entities.TryGetValue(entity.Id, out T internalEntity))
			{
				Entities.Remove(entity.Id);

				foreach (UniqueEntityIndex<T> index in Indices)
					index.EntityDeleted(internalEntity);
			}
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

		private class Subscription : DisposableBase
		{
			private readonly MemoryRepositoryBase<T> _repo;
			private readonly string _id;

			public Subscription(MemoryRepositoryBase<T> repo, string id)
			{
				_repo = repo;
				_id = id;
			}

			protected override void DisposeManagedResources()
			{
				using (_repo.Lock.WriterLock())
					_repo._changeListeners.Remove(_id);
			}
		}
	}
}
