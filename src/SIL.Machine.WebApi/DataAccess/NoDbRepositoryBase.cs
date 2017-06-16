using System.Collections.Generic;
using System.Threading.Tasks;
using NoDb;
using SIL.Machine.WebApi.Models;
using SIL.Threading;

namespace SIL.Machine.WebApi.DataAccess
{
	public class NoDbRepositoryBase<T> : IRepository<T> where T : class, IEntity<T>
	{
		protected const string NoDbProjectId = "machine";

		protected NoDbRepositoryBase(IBasicCommands<T> commands, IBasicQueries<T> queries)
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
			AsyncContext.Run(() => UpdateAsync(entity));
		}

		public void Delete(T entity, bool checkConflict = false)
		{
			AsyncContext.Run(() => DeleteAsync(entity));
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
			await Commands.DeleteAsync(NoDbProjectId, entity.Id);
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
