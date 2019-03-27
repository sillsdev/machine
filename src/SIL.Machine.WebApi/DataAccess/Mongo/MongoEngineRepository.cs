using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SIL.Machine.WebApi.Configuration;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.Mongo
{
	public class MongoEngineRepository : MongoRepository<Engine>, IEngineRepository
	{
		public MongoEngineRepository(IOptions<MongoDataAccessOptions> options)
			: base(options, "engines")
		{
		}

		public override void Init()
		{
			CreateOrUpdateIndex(new CreateIndexModel<Engine>(Builders<Engine>.IndexKeys
				.Ascending(e => e.SourceLanguageTag)
				.Ascending(e => e.TargetLanguageTag)));
			CreateOrUpdateIndex(new CreateIndexModel<Engine>(Builders<Engine>.IndexKeys
				.Ascending(e => e.Projects)));
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
