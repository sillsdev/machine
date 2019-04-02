using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using NoDb;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.NoDb
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

		public void Init()
		{
		}

		public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default(CancellationToken))
		{
			return await Queries.GetAllAsync(NoDbProjectId, ct);
		}

		public async Task<T> GetAsync(string id, CancellationToken ct = default(CancellationToken))
		{
			return await Queries.FetchAsync(NoDbProjectId, id, ct);
		}

		public async Task<bool> ExistsAsync(string id, CancellationToken ct = default(CancellationToken))
		{
			return await Queries.GetCountAsync(id, ct) > 0;
		}

		public async Task InsertAsync(T entity, CancellationToken ct = default(CancellationToken))
		{
			if (string.IsNullOrEmpty(entity.Id))
				entity.Id = ObjectId.GenerateNewId().ToString();
			await Commands.CreateAsync(NoDbProjectId, entity.Id, entity, ct);
		}

		public async Task UpdateAsync(T entity, bool checkConflict = false,
			CancellationToken ct = default(CancellationToken))
		{
			if (checkConflict)
				await CheckForConcurrencyConflictAsync(entity);
			await Commands.UpdateAsync(NoDbProjectId, entity.Id, entity, ct);
		}

		public async Task DeleteAsync(T entity, bool checkConflict = false,
			CancellationToken ct = default(CancellationToken))
		{
			if (checkConflict)
				await CheckForConcurrencyConflictAsync(entity);
			await DeleteAsync(entity.Id, ct);
		}

		public async Task DeleteAsync(string id, CancellationToken ct = default(CancellationToken))
		{
			await Commands.DeleteAsync(NoDbProjectId, id, ct);
		}

		public Task<Subscription<T>> SubscribeAsync(string id, CancellationToken ct = default(CancellationToken))
		{
			throw new NotSupportedException();
		}

		private async Task CheckForConcurrencyConflictAsync(T entity)
		{
			T internalEntity = await GetAsync(entity.Id);
			if (internalEntity == null)
				throw new ConcurrencyConflictException("The entity has been deleted.");

			if (entity.Revision != internalEntity.Revision)
				throw new ConcurrencyConflictException("The entity has been updated.");
		}
	}
}
