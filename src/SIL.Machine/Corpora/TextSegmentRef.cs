using System;

namespace SIL.Machine.Corpora
{
	public struct TextSegmentRef : IEquatable<TextSegmentRef>, IComparable<TextSegmentRef>
	{
		public TextSegmentRef(int sectionNum, int segmentNum)
		{
			SectionNumber = sectionNum;
			SegmentNumber = segmentNum;
		}

		public int SectionNumber { get; }

		public int SegmentNumber { get; }

		public override bool Equals(object obj)
		{
			return Equals((TextSegmentRef) obj);
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + SectionNumber.GetHashCode();
			code = code * 31 + SegmentNumber.GetHashCode();
			return code;
		}

		public bool Equals(TextSegmentRef other)
		{
			return SectionNumber == other.SectionNumber && SegmentNumber == other.SegmentNumber;
		}

		public int CompareTo(TextSegmentRef other)
		{
			if (SectionNumber != other.SectionNumber)
				return SectionNumber - other.SectionNumber;
			return SegmentNumber - other.SegmentNumber;
		}

		public override string ToString()
		{
			return string.Format("{0}.{1}", SectionNumber, SegmentNumber);
		}
	}
}
