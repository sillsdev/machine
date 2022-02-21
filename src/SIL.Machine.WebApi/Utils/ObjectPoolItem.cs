namespace SIL.Machine.WebApi.Utils;

public class ObjectPoolItem<T> : DisposableBase
{
	private readonly ObjectPool<T> _pool;

	internal ObjectPoolItem(ObjectPool<T> pool, T item)
	{
		_pool = pool;
		Object = item;
	}

	public T Object { get; }

	protected override void DisposeManagedResources()
	{
		_pool.Put(Object);
	}
}
