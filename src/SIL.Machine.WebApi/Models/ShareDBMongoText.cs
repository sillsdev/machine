using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace SIL.Machine.WebApi.Models
{
    public class ShareDBMongoText : IText
    {
	    private readonly string _projectId;
		private readonly BsonDocument _doc;
		private readonly ITokenizer<string, int> _wordTokenizer;

		public ShareDBMongoText(string projectId, BsonDocument doc, ITokenizer<string, int> wordTokenizer)
		{
			_projectId = projectId;
			_doc = doc;
			_wordTokenizer = wordTokenizer;
		}
        public string Id
		{
			get
			{
				var id = (string) _doc["_id"];
				int index = id.IndexOf(":", StringComparison.Ordinal);
				return $"{_projectId}_{id.Substring(0, index)}";
			}
		}

        public IEnumerable<TextSegment> Segments 
		{
			get
			{
				var ops = (BsonArray) _doc["ops"];
				var sb = new StringBuilder();
				foreach (BsonDocument op in ops.Cast<BsonDocument>())
					sb.Append(op["insert"]);

				string[] segments = sb.ToString().Split('\n');
				for (int i = 0; i < segments.Length; i++)
					yield return new TextSegment(new TextSegmentRef(1, i + 1), TokenizationExtensions.TokenizeToStrings(_wordTokenizer, segments[i]).ToArray());
			}
		}
    }
}