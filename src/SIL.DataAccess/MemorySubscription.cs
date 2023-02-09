namespace SIL.DataAccess;

public class MemorySubscription<T> : ISubscription<T> where T : IEntity
{
    private readonly Action<MemorySubscription<T>> _remove;
    private readonly AsyncAutoResetEvent _changeEvent;
    private bool disposedValue;

    public MemorySubscription(T? initialEntity, Action<MemorySubscription<T>> remove)
    {
        _remove = remove;
        _changeEvent = new AsyncAutoResetEvent();
        Change = new EntityChange<T>(
            initialEntity == null ? EntityChangeType.Delete : EntityChangeType.Update,
            initialEntity
        );
    }

    public EntityChange<T> Change { get; private set; }

    public async Task WaitForChangeAsync(TimeSpan? timeout = default, CancellationToken cancellationToken = default)
    {
        if (timeout is null)
            timeout = Timeout.InfiniteTimeSpan;
        await TaskTimeout(ct => _changeEvent.WaitAsync(ct), timeout.Value, cancellationToken);
    }

    internal void HandleChange(EntityChange<T> change)
    {
        Change = change;
        _changeEvent.Set();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _remove(this);
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

    private static async Task TaskTimeout(
        Func<CancellationToken, Task> action,
        TimeSpan timeout,
        CancellationToken cancellationToken = default
    )
    {
        if (timeout == Timeout.InfiniteTimeSpan)
        {
            await action(cancellationToken);
        }
        else
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task task = action(cts.Token);
            Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, cancellationToken));
            if (task != completedTask)
                cts.Cancel();
            await completedTask;
        }
    }
}
