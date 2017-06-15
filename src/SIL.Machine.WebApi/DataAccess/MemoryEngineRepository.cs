using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess
{
	public class MemoryEngineRepository : MemoryRepositoryBase<Engine>, IEngineRepository
	{
		public Task<Engine> GetByLanguageTagAsync(string sourceLanguageTag, string targetLanguageTag)
		{
			Engine engine = Entities.Values.Select(ew => ew.Entity).SingleOrDefault(e => e.IsShared
				&& e.SourceLanguageTag == sourceLanguageTag && e.TargetLanguageTag == targetLanguageTag);
			return Task.FromResult(engine?.Clone());
		}

		public Task<Engine> GetByProjectIdAsync(string projectId)
		{
			Engine engine = Entities.Values.Select(ew => ew.Entity).SingleOrDefault(e => e.Projects.Contains(projectId));
			return Task.FromResult(engine?.Clone());
		}
	}
}
