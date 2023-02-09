namespace SIL.DataAccess;

public class MongoSubscription<T> : ISubscription<T> where T : IEntity
{
    private readonly IMongoCollection<T> _entities;
    private readonly IMongoCollection<ChangeEvent> _changeEvents;
    private readonly Func<T, bool> _filter;
    private bool disposedValue;

    public MongoSubscription(
        IMongoCollection<T> entities,
        IMongoCollection<ChangeEvent> changeEvents,
        Func<T, bool> filter,
        T? initialEntity
    )
    {
        _entities = entities;
        _changeEvents = changeEvents;
        _filter = filter;
        Change = new EntityChange<T>(
            initialEntity == null ? EntityChangeType.Delete : EntityChangeType.Update,
            initialEntity
        );
    }

    public EntityChange<T> Change { get; private set; }

    EntityChange<T> ISubscription<T>.Change => throw new NotImplementedException();

    public async Task WaitForChangeAsync(TimeSpan? timeout = default, CancellationToken cancellationToken = default)
    {
        Expression<Func<ChangeEvent, bool>> changeEventFilter;
        if (Change.Entity is null)
            changeEventFilter = ce => ce.ChangeType == EntityChangeType.Insert;
        else
            changeEventFilter = ce => ce.EntityRef == Change.Entity.Id && ce.Revision > Change.Entity.Revision;
        using IAsyncCursor<ChangeEvent> cursor = await _changeEvents
            .Find(changeEventFilter, new FindOptions { MaxAwaitTime = timeout, CursorType = CursorType.TailableAwait })
            .ToCursorAsync(cancellationToken);
        DateTime started = DateTime.UtcNow;

        while (await cursor.MoveNextAsync(cancellationToken))
        {
            bool entityNotFound = Change.Entity is null;
            bool changed = false;
            foreach (ChangeEvent ce in cursor.Current)
            {
                T? entity = await _entities
                    .AsQueryable()
                    .FirstOrDefaultAsync(e => e.Id == ce.EntityRef && e.Revision == ce.Revision, cancellationToken);
                if (entityNotFound)
                {
                    if (entity is not null && _filter(entity))
                    {
                        Change = new EntityChange<T>(ce.ChangeType, entity);
                        changed = true;
                    }
                }
                else
                {
                    Change = new EntityChange<T>(ce.ChangeType, entity);
                    changed = true;
                }
            }

            if (changed)
                return;

            if (timeout.HasValue && DateTime.UtcNow - started >= timeout)
                return;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
