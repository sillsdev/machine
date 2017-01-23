using SIL.Machine.Translation;

namespace SIL.Machine.Corpora
{
	public class TextAlignment
	{
		public TextAlignment(TextSegmentRef segRef, WordAlignmentMatrix alignment)
		{
			SegmentRef = segRef;
			Alignment = alignment;
		}

		public TextSegmentRef SegmentRef { get; }

		public WordAlignmentMatrix Alignment { get; }
	}
}
