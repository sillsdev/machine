using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class MemoryTextAlignmentCollection : ITextAlignmentCollection
	{
		public MemoryTextAlignmentCollection(string id)
			: this(id, Enumerable.Empty<TextAlignment>())
		{
		}

		public MemoryTextAlignmentCollection(string id, IEnumerable<TextAlignment> alignments)
		{
			Id = id;
			Alignments = alignments.ToArray();
		}

		public string Id { get; }

		public string SortKey => Id;

		public IEnumerable<TextAlignment> Alignments { get; }

		public ITextAlignmentCollection Invert()
		{
			return new MemoryTextAlignmentCollection(Id,
				Alignments.Select(ta => new TextAlignment(ta.SegmentRef,
					ta.AlignedWordPairs.Select(wp => new AlignedWordPair(wp.TargetIndex, wp.SourceIndex)))));
		}
	}
}
