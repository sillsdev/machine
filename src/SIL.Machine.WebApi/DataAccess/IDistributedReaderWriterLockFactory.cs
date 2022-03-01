namespace SIL.Machine.WebApi.DataAccess;

public interface IDistributedReaderWriterLockFactory
{
	IDistributedReaderWriterLock Create(string name);
	ValueTask<bool> DeleteAsync(string name);
}
