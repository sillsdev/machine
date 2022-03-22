using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class AlignmentRow
	{
		public AlignmentRow(string textId, object segRef)
		{
			TextId = textId;
			Ref = segRef;
		}

		public string TextId { get; }

		public object Ref { get; }

		public IReadOnlyCollection<AlignedWordPair> AlignedWordPairs { get; set; } = Array.Empty<AlignedWordPair>();

		public AlignmentRow Invert()
		{
			return new AlignmentRow(TextId, Ref)
			{
				AlignedWordPairs = new HashSet<AlignedWordPair>(AlignedWordPairs.Select(wp => wp.Invert()))
			};
		}
	}
}
