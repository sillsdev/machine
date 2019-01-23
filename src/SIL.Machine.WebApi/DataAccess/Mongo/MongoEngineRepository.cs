using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.Mongo
{
	public class MongoEngineRepository : MongoRepository<Engine>, IEngineRepository
	{
		public MongoEngineRepository(IMongoCollection<Engine> collection)
			: base(collection)
		{
		}

		public Task<Engine> GetByLanguageTagAsync(string sourceLanguageTag, string targetLanguageTag,
			CancellationToken ct = default(CancellationToken))
		{
			return Collection.Find(e => e.SourceLanguageTag == sourceLanguageTag
				&& e.TargetLanguageTag == targetLanguageTag && e.IsShared).FirstOrDefaultAsync(ct);
		}

		public Task<Engine> GetByProjectIdAsync(string projectId, CancellationToken ct = default(CancellationToken))
		{
			return Collection.Find(e => e.Projects.Contains(projectId)).FirstOrDefaultAsync(ct);
		}
	}
}
