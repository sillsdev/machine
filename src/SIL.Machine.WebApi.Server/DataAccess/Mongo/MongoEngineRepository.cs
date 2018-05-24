using System.Threading.Tasks;
using MongoDB.Driver;
using SIL.Machine.WebApi.Server.Models;

namespace SIL.Machine.WebApi.Server.DataAccess.Mongo
{
	public class MongoEngineRepository : MongoRepository<Engine>, IEngineRepository
	{
		public MongoEngineRepository(IMongoCollection<Engine> collection)
			: base(collection)
		{
		}

		public Task<Engine> GetByLanguageTagAsync(string sourceLanguageTag, string targetLanguageTag)
		{
			return Collection.Find(e => e.SourceLanguageTag == sourceLanguageTag
				&& e.TargetLanguageTag == targetLanguageTag && e.IsShared).FirstOrDefaultAsync();
		}

		public Task<Engine> GetByProjectIdAsync(string projectId)
		{
			return Collection.Find(e => e.Projects.Contains(projectId)).FirstOrDefaultAsync();
		}
	}
}
