using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace SIL.Machine.WebApi.Models
{
	public class ShareDBMongoTextCorpus : ITextCorpus
	{
		private readonly IMongoCollection<BsonDocument> _collection;
		private readonly bool _isSource;
		private readonly ITokenizer<string, int> _wordTokenizer;

		public ShareDBMongoTextCorpus(MongoClient mongoClient, string projectId, bool isSource, ITokenizer<string, int> wordTokenizer)
		{
			IMongoDatabase db = mongoClient.GetDatabase("realtime");
			_collection = db.GetCollection<BsonDocument>(projectId);
			_isSource = isSource;
			_wordTokenizer = wordTokenizer;
		}

		public IEnumerable<IText> Texts
		{
			get
			{
				string suffix = _isSource ? "source" : "target";
				FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Regex("_id", $"^.+:{suffix}$");
				IAsyncCursor<BsonDocument> cursor = _collection.Find(filter).ToCursor();
				return cursor.ToEnumerable().Select(d => new ShareDBMongoText(d, _wordTokenizer));
			}
		}

		public bool TryGetText(string id, out IText text)
		{
			string suffix = _isSource ? "source" : "target";
			FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", $"{id}:{suffix}");
			BsonDocument doc = _collection.Find(filter).FirstOrDefault();
			if (doc != null)
			{
				text = new ShareDBMongoText(doc, _wordTokenizer);
				return true;
			}

			text = null;
			return false;
		}

		public IText GetText(string id)
		{
			IText text;
			if (TryGetText(id, out text))
				return text;
			throw new ArgumentException("The a text with the specified Id does not exist.", nameof(id));
		}
	}

}