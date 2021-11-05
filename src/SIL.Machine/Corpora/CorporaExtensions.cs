using System.Collections.Generic;
using System.Linq;
using System.Text;

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

		public static StringBuilder TrimEnd(this StringBuilder sb)
		{
			if (sb.Length == 0)
				return sb;

			int i = sb.Length - 1;
			for (; i >= 0; i--)
			{
				if (!char.IsWhiteSpace(sb[i]))
					break;
			}

			if (i < sb.Length - 1)
				sb.Length = i + 1;

			return sb;
		}
	}
}
