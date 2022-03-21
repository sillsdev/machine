using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class TextAlignmentCorpusRow
	{
		public TextAlignmentCorpusRow(string textId, object segRef)
		{
			TextId = textId;
			Ref = segRef;
		}

		public string TextId { get; }

		public object Ref { get; }

		public IReadOnlyCollection<AlignedWordPair> AlignedWordPairs { get; set; } = Array.Empty<AlignedWordPair>();

		public TextAlignmentCorpusRow Invert()
		{
			return new TextAlignmentCorpusRow(TextId, Ref)
			{
				AlignedWordPairs = new HashSet<AlignedWordPair>(AlignedWordPairs.Select(wp => wp.Invert()))
			};
		}
	}
}
