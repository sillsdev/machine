using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.Memory
{
	public class MemoryProjectRepository : MemoryRepository<Project>, IProjectRepository
	{
		public MemoryProjectRepository()
			: this(null)
		{
		}

		internal MemoryProjectRepository(IProjectRepository persistenceRepo)
			: base(persistenceRepo)
		{
		}
	}
}
