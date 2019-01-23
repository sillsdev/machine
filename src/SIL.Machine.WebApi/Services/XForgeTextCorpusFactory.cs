using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Options;

namespace SIL.Machine.WebApi.Services
{
	public class XForgeTextCorpusFactory : ITextCorpusFactory
	{
		private readonly IMongoClient _mongoClient;
		private readonly IRepository<Project> _projectRepo;

		public XForgeTextCorpusFactory(IOptions<XForgeTextCorpusOptions> options,
			IRepository<Project> projectRepo)
		{
			_mongoClient = new MongoClient(options.Value.MongoConnectionString ?? "mongodb://localhost:27017");
			_projectRepo = projectRepo;
		}

		public async Task<ITextCorpus> CreateAsync(IEnumerable<string> projects, TextCorpusType type)
		{
			return new DictionaryTextCorpus(await CreateTextsAsync(projects, type));
		}

		private async Task<IReadOnlyList<IText>> CreateTextsAsync(IEnumerable<string> projects,
			TextCorpusType type)
		{
			StringTokenizer wordTokenizer = new LatinWordTokenizer();
			IMongoDatabase sfDatabase = _mongoClient.GetDatabase("scriptureforge");
			IMongoDatabase realtimeDatabase = _mongoClient.GetDatabase("realtime");
			IMongoCollection<BsonDocument> projectsColl = sfDatabase.GetCollection<BsonDocument>("projects");
			var texts = new List<IText>();
			foreach (string projectId in projects)
			{
				Project project = await _projectRepo.GetAsync(projectId);
				if (project == null)
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
				StringTokenizer segmentTokenizer = null;
				if (segmentType != null)
					segmentTokenizer = WebApiUtils.CreateSegmentTokenizer(segmentType);

				FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id",
					ObjectId.Parse(projectId));
				BsonDocument projectDoc = await projectsColl.Find(filter).FirstOrDefaultAsync();
				if (projectDoc == null)
					continue;
				var code = "sf_" + (string) projectDoc["projectCode"];
				var config = (BsonDocument) projectDoc["config"];
				bool isScripture = false;
				if (config.TryGetValue("isTranslationDataScripture", out BsonValue isScriptureValue))
					isScripture = (bool) isScriptureValue;

				IMongoCollection<BsonDocument> projectColl = realtimeDatabase.GetCollection<BsonDocument>(code);
				IMongoDatabase projectDatabase = _mongoClient.GetDatabase(code);
				IMongoCollection<BsonDocument> translateColl = projectDatabase.GetCollection<BsonDocument>("translate");
				filter = Builders<BsonDocument>.Filter.Eq("isDeleted", false);
				using (IAsyncCursor<BsonDocument> cursor = await translateColl.Find(filter).ToCursorAsync())
				{
					while (await cursor.MoveNextAsync())
					{
						foreach (BsonDocument docInfo in cursor.Current)
						{
							var id = (ObjectId) docInfo["_id"];
							filter = Builders<BsonDocument>.Filter.Eq("_id", $"{id}:{suffix}");
							BsonDocument doc = await projectColl.Find(filter).FirstAsync();
							if (isScripture)
								texts.Add(new XForgeScriptureText(wordTokenizer, project.Id, doc));
							else
								texts.Add(new XForgeRichText(segmentTokenizer, wordTokenizer, project.Id, doc));
						}
					}
				}
			}

			return texts;
		}
	}
}
