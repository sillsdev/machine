using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Tokenization;

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

		public ITokenizer<string, int, string> WordTokenizer { get; }

		public abstract IEnumerable<TextSegment> GetSegments(bool includeText = true, IText sortBasedOn = null);

		protected TextSegment CreateTextSegment(bool includeText, string text, object segRef,
			bool isSentenceStart = true, bool isInRange = false, bool isRangeStart = false)
		{
			text = text.Trim();
			if (!includeText)
			{
				return new TextSegment(Id, segRef, Array.Empty<string>(), isSentenceStart, isInRange, isRangeStart,
					isEmpty: text.Length == 0);
			}
			IReadOnlyList<string> segment = WordTokenizer.Tokenize(text).ToArray();
			segment = TokenProcessors.UnescapeSpaces.Process(segment);
			return new TextSegment(Id, segRef, segment, isSentenceStart, isInRange, isRangeStart, segment.Count == 0);
		}

		protected TextSegment CreateEmptyTextSegment(object segRef, bool isInRange = false)
		{
			return new TextSegment(Id, segRef, Array.Empty<string>(), isSentenceStart: true, isInRange,
				isRangeStart: false, isEmpty: true);
		}
	}
}
