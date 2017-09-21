using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.WebApi.Server.Options;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.DataAccess;

namespace SIL.Machine.WebApi.Server.Services
{
	public class ShareDBMongoTextCorpusFactory : ITextCorpusFactory
	{
		private readonly IMongoClient _mongoClient;
		private readonly IRepository<Project> _projectRepo;

		public ShareDBMongoTextCorpusFactory(IOptions<ShareDBMongoTextCorpusOptions> options,
			IRepository<Project> projectRepo)
		{
			_mongoClient = new MongoClient(options.Value.MongoConnectionString);
			_projectRepo = projectRepo;
		}

		public ITextCorpus Create(IEnumerable<string> projects, TextCorpusType type)
		{
			return new DictionaryTextCorpus(CreateTexts(projects, type).ToArray());
		}

		private IEnumerable<ShareDBMongoText> CreateTexts(IEnumerable<string> projects, TextCorpusType type)
		{
			StringTokenizer wordTokenizer = new LatinWordTokenizer();
			IMongoDatabase db = _mongoClient.GetDatabase("realtime");
			foreach (string projectId in projects)
			{
				if (!_projectRepo.TryGet(projectId, out Project project))
					continue;

				string segmentType = null;
				string suffix = null;
				switch (type)
				{
					case TextCorpusType.Source:
						suffix = "source";
						segmentType = project.SourceSegmentType;
						break;
					case TextCorpusType.Target:
						suffix = "target";
						segmentType = project.TargetSegmentType;
						break;
				}

				StringTokenizer segmentTokenizer = WebApiUtils.CreateSegmentTokenizer(segmentType);

				IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>(project.Id);
				FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Regex("_id", $"^.+:{suffix}$");
				IAsyncCursor<BsonDocument> cursor = collection.Find(filter).ToCursor();

				foreach (BsonDocument doc in cursor.ToEnumerable())
					yield return new ShareDBMongoText(segmentTokenizer, wordTokenizer, project.Id, doc);
			}
		}
	}
}
