using SIL.ObjectModel;

namespace SIL.Machine.WebApi.DataAccess;

public class MongoSubscription<T> : DisposableBase, ISubscription<T> where T : IEntity
{
    private readonly IMongoCollection<T> _entities;
    private readonly IMongoCollection<ChangeEvent> _changeEvents;
    private readonly Func<T, bool> _filter;

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
}
