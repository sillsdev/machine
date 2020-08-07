using System.Collections.Generic;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;

namespace SIL.Machine.Corpora
{
	public abstract class TextBase : IText
	{
		protected TextBase(ITokenizer<string, int> wordTokenizer, string id, string sortKey)
		{
			WordTokenizer = wordTokenizer;
			Id = id;
			SortKey = sortKey;
		}

		public string Id { get; }

		public string SortKey { get; }

		protected ITokenizer<string, int> WordTokenizer { get; }

		public abstract IEnumerable<TextSegment> Segments { get; }

		protected TextSegment CreateTextSegment(string text, object segRef, bool inRange = false)
		{
			IReadOnlyList<string> segment = WordTokenizer.TokenizeToStrings(text.Trim().Normalize())
				.Process(StringProcessors.UnescapeSpaces);
			return new TextSegment(segRef, segment, inRange);
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
