namespace SIL.Machine.WebApi.DataAccess.Memory;

public class MemoryEngineRepository : MemoryRepository<Engine>, IEngineRepository
{
	public MemoryEngineRepository()
		: this(null)
	{
	}

	internal MemoryEngineRepository(IEngineRepository persistenceRepo)
		: base(persistenceRepo)
	{
	}
}
