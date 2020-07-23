using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public static class CorporaExtensions
	{
		public static IEnumerable<TextSegment> GetSegments(this ITextCorpus corpus)
		{
			return corpus.Texts.SelectMany(t => t.Segments);
		}
	}
}
