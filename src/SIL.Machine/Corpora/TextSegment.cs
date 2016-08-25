namespace SIL.Machine.Corpora
{
	public class TextSegment
	{
		public TextSegment(TextSegmentRef segRef, string value)
		{
			SegmentRef = segRef;
			Value = value;
		}

		public TextSegmentRef SegmentRef { get; }

		public bool IsEmpty => string.IsNullOrEmpty(Value);

		public string Value { get; }

		public override string ToString()
		{
			return string.Format("{0} - {1}", SegmentRef, Value);
		}
	}
}
