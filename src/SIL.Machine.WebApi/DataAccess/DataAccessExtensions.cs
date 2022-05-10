namespace SIL.Machine.WebApi.DataAccess;

public static class DataAccessExtensions
{
	public static async Task<T?> GetAsync<T>(this IRepository<T> repo, string id,
		CancellationToken cancellationToken = default) where T : IEntity
	{
		Attempt<T> attempt = await repo.TryGetAsync(id, cancellationToken);
		if (attempt.Success)
			return attempt.Result;
		return default;
	}

	public static async Task<IReadOnlyList<T>> GetAllAsync<T>(this IRepository<T> repo,
		CancellationToken cancellationToken = default) where T : IEntity
	{
		return await repo.GetAllAsync(e => true, cancellationToken);
	}

	public static async Task<Attempt<T>> TryGetAsync<T>(this IRepository<T> repo, string id,
		CancellationToken cancellationToken = default) where T : IEntity
	{
		T? entity = await repo.GetAsync(e => e.Id == id, cancellationToken);
		return new Attempt<T>(entity != null, entity);
	}

	public static async Task<bool> ExistsAsync<T>(this IRepository<T> repo, string id,
		CancellationToken cancellationToken = default) where T : IEntity
	{
		return await repo.ExistsAsync(e => e.Id == id, cancellationToken);
	}

	public static Task<T?> UpdateAsync<T>(this IRepository<T> repo, string id, Action<IUpdateBuilder<T>> update,
		bool upsert = false, CancellationToken cancellationToken = default) where T : IEntity
	{
		return repo.UpdateAsync(e => e.Id == id, update, upsert, cancellationToken);
	}

	public static Task<T?> UpdateAsync<T>(this IRepository<T> repo, T entity, Action<IUpdateBuilder<T>> update,
		bool upsert = false, CancellationToken cancellationToken = default) where T : IEntity
	{
		return repo.UpdateAsync(entity.Id, update, upsert, cancellationToken);
	}

	public static Task<T?> DeleteAsync<T>(this IRepository<T> repo, string id,
		CancellationToken cancellationToken = default) where T : IEntity
	{
		return repo.DeleteAsync(e => e.Id == id, cancellationToken);
	}

	public static async Task<bool> DeleteAsync<T>(this IRepository<T> repo, T entity,
		CancellationToken cancellationToken = default) where T : class, IEntity
	{
		return (await repo.DeleteAsync(e => e.Id == entity.Id, cancellationToken)) != null;
	}

	public static async Task CreateOrUpdateAsync<T>(this IMongoIndexManager<T> indexes, CreateIndexModel<T> indexModel)
	{
		try
		{
			await indexes.CreateOneAsync(indexModel);
		}
		catch (MongoCommandException ex)
		{
			if (ex.CodeName == "IndexOptionsConflict")
			{
				string name = ex.Command["indexes"][0]["name"].AsString;
				await indexes.DropOneAsync(name);
				await indexes.CreateOneAsync(indexModel);
			}
			else
			{
				throw;
			}
		}
	}
}
