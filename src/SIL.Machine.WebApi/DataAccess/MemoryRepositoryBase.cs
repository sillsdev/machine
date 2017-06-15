using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;
using SIL.Threading;

namespace SIL.Machine.WebApi.DataAccess
{
	public class MemoryRepositoryBase<T> : IRepository<T> where T : class, IEntity<T>
	{
		protected ConcurrentDictionary<string, EntityWrapper> Entities { get; } = new ConcurrentDictionary<string, EntityWrapper>();

		public IEnumerable<T> GetAll()
		{
			return Entities.Values.Select(ew => ew.Entity.Clone()).ToArray();
		}

		public bool TryGet(string id, out T entity)
		{
			if (Entities.TryGetValue(id, out EntityWrapper ew))
			{
				entity = ew.Entity.Clone();
				return true;
			}

			entity = null;
			return false;
		}

		public void Insert(T entity)
		{
			if (string.IsNullOrEmpty(entity.Id))
				entity.Id = ObjectId.GenerateNewId().ToString();
			if (!Entities.TryAdd(entity.Id, new EntityWrapper(entity.Clone())))
				throw new InvalidOperationException("An entity with the specified identifier already exists.");
		}

		public void Update(T entity)
		{
			T oldEntity = entity.Clone();
			entity.Revision++;
			if (!Entities.TryUpdate(entity.Id, new EntityWrapper(entity.Clone()), new EntityWrapper(oldEntity)))
				throw new ConcurrencyConflictException("The entity has been updated or deleted in another thread.");
		}

		public void Delete(T entity)
		{
			if (Entities.TryRemove(entity.Id, out EntityWrapper oldWrapper))
			{
				if (entity.Revision != oldWrapper.Entity.Revision)
					throw new ConcurrencyConflictException("The entity has been updated in another thread.");
			}
			else
			{
				throw new ConcurrencyConflictException("The entity has been deleted in another thread.");
			}
		}

		public Task<IEnumerable<T>> GetAllAsync()
		{
			return Task.FromResult(GetAll());
		}

		public Task<T> GetAsync(string id)
		{
			if (TryGet(id, out T entity))
				return Task.FromResult(entity);
			return Task.FromResult<T>(null);
		}

		public Task InsertAsync(T entity)
		{
			Insert(entity);
			return TaskConstants<object>.Default;
		}

		public Task UpdateAsync(T entity)
		{
			Update(entity);
			return TaskConstants<object>.Default;
		}

		public Task DeleteAsync(T entity)
		{
			Delete(entity);
			return TaskConstants<object>.Default;
		}

		protected struct EntityWrapper : IEquatable<EntityWrapper>
		{
			public EntityWrapper(T entity)
			{
				Entity = entity;
			}

			public T Entity { get; }

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj))
					return false;
				return obj is EntityWrapper && Equals((EntityWrapper) obj);
			}

			public bool Equals(EntityWrapper other)
			{
				return Entity.Id == other.Entity.Id && Entity.Revision == other.Entity.Revision;
			}

			public override int GetHashCode()
			{
				int code = 23;
				code = code * 31 + Entity.Id.GetHashCode();
				code = code * 31 + Entity.Revision.GetHashCode();
				return code;
			}
		}
	}
}
