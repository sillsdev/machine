namespace SIL.Machine.WebApi.DataAccess;

public static class DataAccessExtensions
{
	public static async Task<T> GetAsync<T>(this IRepository<T> repo, string id,
		CancellationToken cancellationToken = default) where T : IEntity<T>
	{
		Attempt<T> attempt = await repo.TryGetAsync(id, cancellationToken);
		if (attempt.Success)
			return attempt.Result;
		return default;
	}

	public static async Task<IReadOnlyList<T>> GetAllAsync<T>(this IRepository<T> repo,
		CancellationToken cancellationToken = default) where T : IEntity<T>
	{
		return await repo.GetAllAsync(e => true, cancellationToken);
	}

	public static async Task<Attempt<T>> TryGetAsync<T>(this IRepository<T> repo, string id,
		CancellationToken cancellationToken = default) where T : IEntity<T>
	{
		T entity = await repo.GetAsync(e => e.Id == id, cancellationToken);
		return new Attempt<T>(entity != null, entity);
	}

	public static async Task<bool> ExistsAsync<T>(this IRepository<T> repo, string id,
		CancellationToken cancellationToken = default) where T : IEntity<T>
	{
		return await repo.ExistsAsync(e => e.Id == id, cancellationToken);
	}

	public static Task<T> UpdateAsync<T>(this IRepository<T> repo, string id, Action<IUpdateBuilder<T>> update,
		bool upsert = false, CancellationToken cancellationToken = default) where T : IEntity<T>
	{
		return repo.UpdateAsync(e => e.Id == id, update, upsert, cancellationToken);
	}

	public static Task<T> UpdateAsync<T>(this IRepository<T> repo, T entity, Action<IUpdateBuilder<T>> update,
		bool upsert = false, CancellationToken cancellationToken = default) where T : IEntity<T>
	{
		return repo.UpdateAsync(entity.Id, update, upsert, cancellationToken);
	}

	public static Task<T> DeleteAsync<T>(this IRepository<T> repo, string id,
		CancellationToken cancellationToken = default) where T : IEntity<T>
	{
		return repo.DeleteAsync(e => e.Id == id, cancellationToken);
	}

	public static async Task<bool> DeleteAsync<T>(this IRepository<T> repo, T entity,
		CancellationToken cancellationToken = default) where T : IEntity<T>
	{
		return (await repo.DeleteAsync(e => e.Id == entity.Id, cancellationToken)) != null;
	}

	public static Task<Build> GetByEngineIdAsync(this IRepository<Build> builds, string engineId,
		CancellationToken cancellationToken = default)
	{
		return builds.GetAsync(b => b.EngineRef == engineId
			&& (b.State == BuildStates.Active || b.State == BuildStates.Pending), cancellationToken);
	}

	public static Task<EntityChange<Build>> GetNewerRevisionAsync(this IRepository<Build> builds,
		string id, long minRevision, CancellationToken cancellationToken = default)
	{
		return builds.GetNewerRevisionAsync(b => b.Id == id, minRevision, cancellationToken);
	}

	public static Task<EntityChange<Build>> GetNewerRevisionByEngineIdAsync(this IRepository<Build> builds,
		string engineId, long minRevision, CancellationToken cancellationToken = default)
	{
		return builds.GetNewerRevisionAsync(b => b.EngineRef == engineId
			&& (b.State == BuildStates.Active || b.State == BuildStates.Pending), minRevision, cancellationToken);
	}

	public static async Task<EntityChange<Build>> GetNewerRevisionAsync(this IRepository<Build> builds,
		Expression<Func<Build, bool>> filter, long minRevision, CancellationToken cancellationToken = default)
	{
		using Subscription<Build> subscription = await builds.SubscribeAsync(filter, cancellationToken);
		EntityChange<Build> curChange = subscription.Change;
		if (curChange.Type == EntityChangeType.Delete && minRevision > 0)
			return curChange;
		while (true)
		{
			if (curChange.Type != EntityChangeType.Delete && minRevision <= curChange.Entity.Revision)
				return curChange;
			await subscription.WaitForUpdateAsync(cancellationToken);
			curChange = subscription.Change;
			if (curChange.Type == EntityChangeType.Delete)
				return curChange;
		}
	}

	public static void CreateOrUpdate<T>(this IMongoIndexManager<T> indexes, CreateIndexModel<T> indexModel)
	{
		try
		{
			indexes.CreateOne(indexModel);
		}
		catch (MongoCommandException ex)
		{
			if (ex.CodeName == "IndexOptionsConflict")
			{
				string name = ex.Command["indexes"][0]["name"].AsString;
				indexes.DropOne(name);
				indexes.CreateOne(indexModel);
			}
			else
			{
				throw;
			}
		}
	}
}
