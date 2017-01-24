using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Translation;

namespace SIL.Machine.Corpora
{
	public class MemoryTextAlignmentCollection : ITextAlignmentCollection
	{
		public MemoryTextAlignmentCollection(string id, IEnumerable<TextAlignment> alignments)
		{
			Id = id;
			Alignments = alignments.ToArray();
		}

		public string Id { get; }

		public IEnumerable<TextAlignment> Alignments { get; }

		public ITextAlignmentCollection Invert()
		{
			return new MemoryTextAlignmentCollection(Id, Alignments.Select(ta =>
				{
					WordAlignmentMatrix newMatrix = ta.Alignment.Clone();
					newMatrix.Transpose();
					return new TextAlignment(ta.SegmentRef, newMatrix);
				}));
		}
	}
}
