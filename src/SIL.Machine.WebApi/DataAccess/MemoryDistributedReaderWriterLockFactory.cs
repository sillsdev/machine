namespace SIL.Machine.WebApi.DataAccess;

public class MemoryDistributedReaderWriterLockFactory : IDistributedReaderWriterLockFactory
{
	private readonly ConcurrentDictionary<string, MemoryDistributedReaderWriterLock> _locks;

	public MemoryDistributedReaderWriterLockFactory()
	{
		_locks = new ConcurrentDictionary<string, MemoryDistributedReaderWriterLock>();
	}

	public IDistributedReaderWriterLock Create(string name)
	{
		return _locks.GetOrAdd(name, name => new MemoryDistributedReaderWriterLock());
	}

	public ValueTask<bool> DeleteAsync(string name)
	{
		return ValueTask.FromResult(_locks.TryRemove(name, out _));
	}
}
