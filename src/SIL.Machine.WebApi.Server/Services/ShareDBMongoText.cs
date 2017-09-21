using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace SIL.Machine.WebApi.Server.Services
{
	public class ShareDBMongoText : IText
	{
		private readonly string _projectId;
		private readonly BsonDocument _doc;
		private readonly ITokenizer<string, int> _segmentTokenizer;
		private readonly ITokenizer<string, int> _wordTokenizer;

		public ShareDBMongoText(ITokenizer<string, int> segmentTokenizer, ITokenizer<string, int> wordTokenizer,
			string projectId, BsonDocument doc)
		{
			_segmentTokenizer = segmentTokenizer;
			_wordTokenizer = wordTokenizer;
			_projectId = projectId;
			_doc = doc;
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
				{
					if (op.TryGetValue("insert", out BsonValue value))
						sb.Append(value);
				}

				int i = 1;
				foreach (string segment in _segmentTokenizer.TokenizeToStrings(sb.ToString()))
				{
					yield return new TextSegment(new TextSegmentRef(1, i),
						_wordTokenizer.TokenizeToStrings(segment).ToArray());
					i++;
				}
			}
		}
	}
}