namespace SIL.DataAccess;

public class AsyncDisposableBase : DisposableBase, IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();

        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        return default;
    }
}
