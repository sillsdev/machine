using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Options;

namespace SIL.Machine.WebApi.Services
{
	public class ShareDBMongoTextCorpusFactory : ITextCorpusFactory
	{
		private readonly IMongoClient _mongoClient;

		public ShareDBMongoTextCorpusFactory(IOptions<ShareDBMongoTextCorpusOptions> options)
		{
			_mongoClient = new MongoClient(options.Value.MongoConnectionString);
		}

		public ITextCorpus Create(IEnumerable<Project> projects, ITokenizer<string, int> wordTokenizer, TextCorpusType type)
		{
			return new DictionaryTextCorpus(CreateTexts(projects, wordTokenizer, type).ToArray());
		}

		private IEnumerable<ShareDBMongoText> CreateTexts(IEnumerable<Project> projects, ITokenizer<string, int> wordTokenizer, TextCorpusType type)
		{
			IMongoDatabase db = _mongoClient.GetDatabase("realtime");
			string suffix = null;
			switch (type)
			{
				case TextCorpusType.Source:
					suffix = "source";
					break;
				case TextCorpusType.Target:
					suffix = "target";
					break;
			}

			foreach (Project project in projects)
			{
				IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>(project.Id);
				FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Regex("_id", $"^.+:{suffix}$");
				IAsyncCursor<BsonDocument> cursor = collection.Find(filter).ToCursor();

				foreach (BsonDocument doc in cursor.ToEnumerable())
					yield return new ShareDBMongoText(project.Id, doc, wordTokenizer);
			}
		}
	}
}
