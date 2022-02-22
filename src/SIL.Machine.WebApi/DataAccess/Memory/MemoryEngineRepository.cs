namespace SIL.Machine.WebApi.DataAccess.Memory;

public class MemoryEngineRepository : MemoryRepository<Engine>
{
	public MemoryEngineRepository()
		: this(null)
	{
	}

	internal MemoryEngineRepository(IRepository<Engine> persistenceRepo)
		: base(persistenceRepo)
	{
	}
}
