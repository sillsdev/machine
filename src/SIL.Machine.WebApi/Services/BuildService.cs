namespace SIL.Machine.WebApi.Services;

public class BuildService : EntityServiceBase<Build>, IBuildService
{
    public BuildService(IRepository<Build> builds) : base(builds) { }

    public async Task<IEnumerable<Build>> GetAllAsync(string parentId)
    {
        CheckDisposed();

        return await Entities.GetAllAsync(e => e.ParentRef == parentId);
    }

    public Task<Build?> GetActiveAsync(string parentId, CancellationToken cancellationToken = default)
    {
        CheckDisposed();

        return Entities.GetAsync(
            b => b.ParentRef == parentId && (b.State == BuildState.Active || b.State == BuildState.Pending),
            cancellationToken
        );
    }

    public Task<EntityChange<Build>> GetNewerRevisionAsync(
        string id,
        long minRevision,
        CancellationToken cancellationToken = default
    )
    {
        CheckDisposed();

        return GetNewerRevisionAsync(b => b.Id == id, minRevision, cancellationToken);
    }

    public Task<EntityChange<Build>> GetActiveNewerRevisionAsync(
        string parentId,
        long minRevision,
        CancellationToken cancellationToken = default
    )
    {
        CheckDisposed();

        return GetNewerRevisionAsync(
            b => b.ParentRef == parentId && (b.State == BuildState.Active || b.State == BuildState.Pending),
            minRevision,
            cancellationToken
        );
    }

    private async Task<EntityChange<Build>> GetNewerRevisionAsync(
        Expression<Func<Build, bool>> filter,
        long minRevision,
        CancellationToken cancellationToken = default
    )
    {
        using ISubscription<Build> subscription = await Entities.SubscribeAsync(filter, cancellationToken);
        EntityChange<Build> curChange = subscription.Change;
        if (curChange.Type == EntityChangeType.Delete && minRevision > 1)
            return curChange;
        while (true)
        {
            if (curChange.Type != EntityChangeType.Delete && minRevision <= curChange.Entity!.Revision)
                return curChange;
            await subscription.WaitForChangeAsync(cancellationToken: cancellationToken);
            curChange = subscription.Change;
            if (curChange.Type == EntityChangeType.Delete)
                return curChange;
        }
    }
}
