using MongoDB.Bson;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.Machine.WebApi.Server.Services
{
	public class XForgeScriptureText : IText
	{
		public XForgeScriptureText(ITokenizer<string, int> wordTokenizer, string projectId, BsonDocument doc)
		{
			var id = (string) doc["_id"];
			int index = id.IndexOf(":", StringComparison.Ordinal);
			Id = $"{projectId}_{id.Substring(0, index)}";
			Segments = GetSegments(wordTokenizer, doc).OrderBy(s => s.SegmentRef).ToArray();
		}

		public string Id { get; }

		public IEnumerable<TextSegment> Segments { get; }

		private static IEnumerable<TextSegment> GetSegments(ITokenizer<string, int> wordTokenizer, BsonDocument doc)
		{
			string prevRef = null;
			var sb = new StringBuilder();
			var ops = (BsonArray) doc["ops"];
			foreach (BsonDocument op in ops.Cast<BsonDocument>())
			{
				// skip embeds
				if (!op.TryGetValue("insert", out BsonValue value) || value.BsonType != BsonType.String)
					continue;

				if (!op.TryGetValue("attributes", out BsonValue attrsValue))
					continue;

				var attrs = (BsonDocument) attrsValue;
				if (!attrs.TryGetValue("segment", out BsonValue segmentValue))
					continue;

				var curRef = (string) segmentValue;
				if (prevRef != null && prevRef != curRef)
				{
					string[] segment = wordTokenizer.TokenizeToStrings(sb.ToString()).ToArray();
					yield return new TextSegment(prevRef, segment);
					sb.Clear();
				}

				sb.Append((string) value);
				prevRef = curRef;
			}

			if (prevRef != null)
			{
				string[] segment = wordTokenizer.TokenizeToStrings(sb.ToString()).ToArray();
				yield return new TextSegment(prevRef, segment);
			}
		}
	}
}
