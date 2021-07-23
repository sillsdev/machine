using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;

namespace SIL.Machine.Corpora
{
	public abstract class TextBase : IText
	{
		protected TextBase(ITokenizer<string, int, string> wordTokenizer, string id, string sortKey)
		{
			WordTokenizer = wordTokenizer;
			Id = id;
			SortKey = sortKey;
		}

		public string Id { get; }

		public string SortKey { get; }

		protected ITokenizer<string, int, string> WordTokenizer { get; }

		public abstract IEnumerable<TextSegment> GetSegments(bool includeText = true);

		protected TextSegment CreateTextSegment(bool includeText, string text, object segRef, bool sentenceStart = true,
			bool inRange = false, bool rangeStart = false)
		{
			text = text.Trim();
			if (!includeText)
			{
				return TextSegment.CreateNoText(Id, segRef, sentenceStart, inRange, rangeStart,
					isEmpty: text.Length == 0);
			}
			IReadOnlyList<string> segment = WordTokenizer.Tokenize(text).ToArray();
			segment = TokenProcessors.UnescapeSpaces.Process(segment);
			return TextSegment.Create(Id, segRef, segment, sentenceStart, inRange, rangeStart);
		}

		protected TextSegment CreateTextSegment(object segRef, bool inRange = false)
		{
			return TextSegment.CreateNoText(Id, segRef, inRange: inRange);
		}

		protected TextSegment CreateTextSegment(bool includeText, string text, params int[] indices)
		{
			return CreateTextSegment(includeText, text, new TextSegmentRef(indices));
		}
	}
}
