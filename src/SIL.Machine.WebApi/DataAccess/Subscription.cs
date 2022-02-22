namespace SIL.Machine.WebApi.DataAccess;

public class Subscription<T> : DisposableBase where T : IEntity<T>
{
	private readonly Action<Subscription<T>> _remove;
	private readonly AsyncAutoResetEvent _changeEvent;

	public Subscription(T initialEntity, Action<Subscription<T>> remove)
	{
		_remove = remove;
		_changeEvent = new AsyncAutoResetEvent();
		Change = new EntityChange<T>(initialEntity == null ? EntityChangeType.Delete : EntityChangeType.Update,
			initialEntity);
	}

	public EntityChange<T> Change { get; private set; }

	public Task WaitForUpdateAsync(CancellationToken ct = default)
	{
		return _changeEvent.WaitAsync(ct);
	}

	internal void HandleChange(EntityChange<T> change)
	{
		Change = change;
		_changeEvent.Set();
	}

	protected override void DisposeManagedResources()
	{
		_remove(this);
	}
}
