using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess
{
	public class MemoryBuildRepository : MemoryRepositoryBase<Build>, IBuildRepository
	{
		private const string EngineIndexName = "Engine";

		public MemoryBuildRepository()
		{
			Indices.Add(new UniqueEntityIndex<Build>(EngineIndexName, b => b.EngineId));
		}

		public async Task<Build> GetByEngineIdAsync(string engineId)
		{
			using (await Lock.ReaderLockAsync())
			{
				UniqueEntityIndex<Build> index = Indices[EngineIndexName];
				if (index.TryGetEntity(engineId, out Build build))
					return build;
				return null;
			}
		}
	}
}
