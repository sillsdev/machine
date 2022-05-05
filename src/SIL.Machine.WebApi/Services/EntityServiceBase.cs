namespace SIL.Machine.WebApi.Services;

public class EntityServiceBase<T> : AsyncDisposableBase where T : IEntity
{
	protected EntityServiceBase(IRepository<T> entities)
	{
		Entities = entities;
	}

	protected IRepository<T> Entities { get; }

	public Task<T?> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		CheckDisposed();

		return Entities.GetAsync(id, cancellationToken);
	}

	public virtual Task CreateAsync(T entity)
	{
		CheckDisposed();

		return Entities.InsertAsync(entity);
	}

	public virtual async Task<bool> DeleteAsync(string id)
	{
		CheckDisposed();

		return (await Entities.DeleteAsync(id)) is not null;
	}
}
