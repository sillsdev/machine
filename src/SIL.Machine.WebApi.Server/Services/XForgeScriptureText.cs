using MongoDB.Bson;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using System;
using System.Collections.Generic;
using System.Globalization;
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

				BsonDocument attrs = attrsValue.AsBsonDocument;
				if (!attrs.TryGetValue("segment", out BsonValue segmentValue))
					continue;

				string curRef = segmentValue.AsString;
				if (prevRef != null && prevRef != curRef)
				{
					// zero pad chapter and verse numbers, so that segments are sorted correctly
					var segRef = new StringBuilder();
					foreach (string refPart in prevRef.Split('/'))
					{
						if (segRef.Length > 0)
							segRef.Append("/");
						if (refPart.StartsWith("verse"))
						{
							string[] parts = refPart.Split('_');
							int chapter = int.Parse(parts[1], CultureInfo.InvariantCulture);
							string verseStr = parts[2];
							string[] verseParts = verseStr.Split('-');
							int verseStart = int.Parse(verseParts[0], CultureInfo.InvariantCulture);
							segRef.Append($"verse_{chapter:D3}_{verseStart:D3}");
							if (verseParts.Length == 2)
							{
								int verseEnd = int.Parse(verseParts[1], CultureInfo.InvariantCulture);
								segRef.Append($"-{verseEnd:D3}");
							}
						}
						else
						{
							segRef.Append(refPart);
						}
					}
					string[] segment = wordTokenizer.TokenizeToStrings(sb.ToString()).ToArray();
					yield return new TextSegment(segRef.ToString(), segment);
					sb.Clear();
				}

				string text = value.AsString;
				// skip blanks
				if (text != "\u00a0" && text != "\u2003\u2003")
					sb.Append(text);
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
