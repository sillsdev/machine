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
		protected MemoryRepositoryBase()
		{
			Lock = new AsyncReaderWriterLock();
			Entities = new Dictionary<string, T>();
			Indices = new KeyedList<string, UniqueEntityIndex<T>>(i => i.Name);
		}

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
				InsertEntity(entity);
		}

		public void Update(T entity)
		{
			using (Lock.WriterLock())
				UpdateEntity(entity);
		}

		public void Delete(T entity)
		{
			using (Lock.WriterLock())
				DeleteEntity(entity);
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
				InsertEntity(entity);
		}

		public async Task UpdateAsync(T entity)
		{
			using (await Lock.WriterLockAsync())
				UpdateEntity(entity);
		}

		public async Task DeleteAsync(T entity)
		{
			using (await Lock.WriterLockAsync())
				DeleteEntity(entity);
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

		private void UpdateEntity(T entity)
		{
			foreach (UniqueEntityIndex<T> index in Indices)
				index.CheckKeyConflict(entity);
			CheckForConcurrencyConflict(entity);

			entity.Revision++;
			T internalEntity = entity.Clone();
			Entities[entity.Id] = internalEntity;

			foreach (UniqueEntityIndex<T> index in Indices)
				index.EntityUpdated(internalEntity);
		}

		private void DeleteEntity(T entity)
		{
			T internalEntity = CheckForConcurrencyConflict(entity);

			Entities.Remove(entity.Id);

			foreach (UniqueEntityIndex<T> index in Indices)
				index.EntityDeleted(internalEntity);
		}

		private T CheckForConcurrencyConflict(T entity)
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
			return internalEntity;
		}
	}
}
