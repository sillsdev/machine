using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess
{
	public class MemoryBuildRepository : MemoryRepositoryBase<Build>, IBuildRepository
	{
		private const string EngineIndexName = "Engine";

		public MemoryBuildRepository(IBuildRepository persistenceRepo = null)
			: base(persistenceRepo)
		{
			var engineIndex = new UniqueEntityIndex<Build>(EngineIndexName, b => b.EngineId);
			if (PersistenceRepository != null)
				engineIndex.PopulateIndex(PersistenceRepository.GetAll());
			Indices.Add(engineIndex);
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
