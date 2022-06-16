namespace SIL.Machine.WebApi.DataAccess;

public interface IRepository<T> where T : IEntity
{
    Task InitAsync();
    Task<T?> GetAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);

    Task InsertAsync(T entity, CancellationToken cancellationToken = default);
    Task InsertAllAsync(IReadOnlyCollection<T> entities, CancellationToken cancellationToken = default);
    Task<T?> UpdateAsync(
        Expression<Func<T, bool>> filter,
        Action<IUpdateBuilder<T>> update,
        bool upsert = false,
        CancellationToken cancellationToken = default
    );
    Task<T?> DeleteAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
    Task<int> DeleteAllAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
    Task<ISubscription<T>> SubscribeAsync(
        Expression<Func<T, bool>> filter,
        CancellationToken cancellationToken = default
    );
}
