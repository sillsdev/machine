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

		public abstract IEnumerable<TextSegment> Segments { get; }

		protected TextSegment CreateTextSegment(string text, object segRef, bool sentenceStart = true,
			bool inRange = false, bool rangeStart = false)
		{
			IReadOnlyList<string> segment = WordTokenizer.Tokenize(text.Trim()).ToArray();
			segment = TokenProcessors.UnescapeSpaces.Process(segment);
			return new TextSegment(segRef, segment, sentenceStart, inRange, rangeStart);
		}

		protected TextSegment CreateTextSegment(object segRef, bool inRange = false)
		{
			return new TextSegment(segRef, inRange);
		}

		protected TextSegment CreateTextSegment(string text, params int[] indices)
		{
			return CreateTextSegment(text, new TextSegmentRef(indices));
		}
	}
}
