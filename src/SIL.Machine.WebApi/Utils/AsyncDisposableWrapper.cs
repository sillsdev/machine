namespace SIL.Machine.WebApi.Utils;

public class AsyncDisposableWrapper : AsyncDisposableBase
{
	private readonly IDisposable _disposable;

	public AsyncDisposableWrapper(IDisposable disposable)
	{
		_disposable = disposable;
	}

	protected override void DisposeManagedResources()
	{
		_disposable.Dispose();
	}
}
