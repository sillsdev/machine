using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class AlignmentRow : IRow
	{
		public AlignmentRow(object segRef)
		{
			Ref = segRef;
		}

		public object Ref { get; }

		public IReadOnlyCollection<AlignedWordPair> AlignedWordPairs { get; set; } = Array.Empty<AlignedWordPair>();

		public AlignmentRow Invert()
		{
			return new AlignmentRow(Ref)
			{
				AlignedWordPairs = new HashSet<AlignedWordPair>(AlignedWordPairs.Select(wp => wp.Invert()))
			};
		}
	}
}
