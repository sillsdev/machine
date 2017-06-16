using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess
{
	public class MemoryEngineRepository : MemoryRepositoryBase<Engine>, IEngineRepository
	{
		private const string LangTagIndexName = "LangTag";
		private const string ProjectIndexName = "Project";

		public MemoryEngineRepository()
		{
			Indices.Add(new UniqueEntityIndex<Engine>(LangTagIndexName, e => (e.SourceLanguageTag, e.TargetLanguageTag),
				e => e.IsShared));
			Indices.Add(new UniqueEntityIndex<Engine>(ProjectIndexName, e => e.Projects));
		}

		public async Task<Engine> GetByLanguageTagAsync(string sourceLanguageTag, string targetLanguageTag)
		{
			using (await Lock.ReaderLockAsync())
			{
				UniqueEntityIndex<Engine> index = Indices[LangTagIndexName];
				if (index.TryGetEntity((sourceLanguageTag, targetLanguageTag), out Engine engine))
					return engine;
				return null;
			}
		}

		public async Task<Engine> GetByProjectIdAsync(string projectId)
		{
			using (await Lock.ReaderLockAsync())
			{
				UniqueEntityIndex<Engine> index = Indices[ProjectIndexName];
				if (index.TryGetEntity(projectId, out Engine engine))
					return engine;
				return null;
			}
		}
	}
}
