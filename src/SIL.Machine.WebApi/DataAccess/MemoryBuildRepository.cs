using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess
{
	public class MemoryBuildRepository : MemoryRepositoryBase<Build>, IBuildRepository
	{
		private readonly IEngineRepository _engineRepo;

		public MemoryBuildRepository(IEngineRepository engineRepo)
		{
			_engineRepo = engineRepo;
		}

		public Task<Build> GetByEngineIdAsync(string engineId)
		{
			Build build = Entities.Values.Select(ew => ew.Entity).SingleOrDefault(b => b.EngineId == engineId);
			return Task.FromResult(build?.Clone());
		}

		public async Task<Build> GetByProjectIdAsync(string projectId)
		{
			Engine engine = await _engineRepo.GetByProjectIdAsync(projectId);
			if (engine == null)
				return null;
			return await GetByEngineIdAsync(engine.Id);
		}
	}
}
