using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NoDb;
using SIL.Machine.WebApi.Server.Models;
using SIL.Threading;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public class NoDbRepository<T> : IRepository<T> where T : class, IEntity<T>
	{
		protected const string NoDbProjectId = "machine";

		public NoDbRepository(IBasicCommands<T> commands, IBasicQueries<T> queries)
		{
			Commands = commands;
			Queries = queries;
		}

		protected IBasicCommands<T> Commands { get; }
		protected IBasicQueries<T> Queries { get; }

		public IEnumerable<T> GetAll()
		{
			return AsyncContext.Run(GetAllAsync);
		}

		public bool TryGet(string id, out T entity)
		{
			entity = AsyncContext.Run(() => GetAsync(id));
			return entity != null;
		}

		public void Insert(T entity)
		{
			AsyncContext.Run(() => InsertAsync(entity));
		}

		public void Update(T entity, bool checkConflict = false)
		{
			AsyncContext.Run(() => UpdateAsync(entity, checkConflict));
		}

		public void Delete(T entity, bool checkConflict = false)
		{
			AsyncContext.Run(() => DeleteAsync(entity, checkConflict));
		}

		public void Delete(string id)
		{
			AsyncContext.Run(() => DeleteAsync(id));
		}

		public async Task<IEnumerable<T>> GetAllAsync()
		{
			return await Queries.GetAllAsync(NoDbProjectId);
		}

		public async Task<T> GetAsync(string id)
		{
			return await Queries.FetchAsync(NoDbProjectId, id);
		}

		public async Task InsertAsync(T entity)
		{
			if (string.IsNullOrEmpty(entity.Id))
				entity.Id = ObjectId.GenerateNewId().ToString();
			await Commands.CreateAsync(NoDbProjectId, entity.Id, entity);
		}

		public async Task UpdateAsync(T entity, bool checkConflict = false)
		{
			if (checkConflict)
				await CheckForConcurrencyConflictAsync(entity);
			await Commands.UpdateAsync(NoDbProjectId, entity.Id, entity);
		}

		public async Task DeleteAsync(T entity, bool checkConflict = false)
		{
			if (checkConflict)
				await CheckForConcurrencyConflictAsync(entity);
			await DeleteAsync(entity.Id);
		}

		public async Task DeleteAsync(string id)
		{
			await Commands.DeleteAsync(NoDbProjectId, id);
		}

		public Task<IDisposable> SubscribeAsync(string id, Action<EntityChange<T>> listener)
		{
			throw new NotSupportedException();
		}

		private async Task CheckForConcurrencyConflictAsync(T entity)
		{
			T internalEntity = await GetAsync(entity.Id);
			if (internalEntity == null)
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
	}
}
