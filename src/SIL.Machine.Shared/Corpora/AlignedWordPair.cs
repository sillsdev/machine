using System;
using System.Text;

namespace SIL.Machine.Corpora
{
	public class AlignedWordPair : IEquatable<AlignedWordPair>
	{
		public AlignedWordPair(int sourceIndex, int targetIndex)
		{
			SourceIndex = sourceIndex;
			TargetIndex = targetIndex;
		}

		public int SourceIndex { get; }
		public int TargetIndex { get; }
		public double TranslationProbability { get; set; } = -1;
		public double AlignmentProbability { get; set; } = -1;

		public bool Equals(AlignedWordPair other)
		{
			return other != null && SourceIndex == other.SourceIndex && TargetIndex == other.TargetIndex;
		}

		public override bool Equals(object obj)
		{
			var other = obj as AlignedWordPair;
			return other != null && Equals(other);
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + SourceIndex.GetHashCode();
			code = code * 31 + TargetIndex.GetHashCode();
			return code;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append($"{SourceIndex}-{TargetIndex}");
			if (TranslationProbability >= 0)
			{
				sb.Append($":{TranslationProbability:0.########}");
				if (AlignmentProbability >= 0)
					sb.Append($":{AlignmentProbability:0.########}");
			}
			return sb.ToString();
		}
	}
}
