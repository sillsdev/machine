using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public static class CorporaExtensions
	{
		public static IEnumerable<TextSegment> GetSegments(this ITextCorpus corpus, bool includeText = true)
		{
			return corpus.Texts.SelectMany(t => t.GetSegments(includeText));
		}

		public static int GetCount(this ITextCorpus corpus, bool nonemptyOnly = false)
		{
			return corpus.Texts.Sum(t => t.GetSegments(includeText: false).Count(s => !nonemptyOnly || !s.IsEmpty));
		}

		public static IText GetText(this ITextCorpus corpus, string id)
		{
			return corpus[id];
		}

		public static ITextAlignmentCollection GetTextAlignmentCollection(this ITextAlignmentCorpus corpus, string id)
		{
			return corpus[id];
		}
	}
}
