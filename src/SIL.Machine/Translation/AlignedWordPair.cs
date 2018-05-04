namespace SIL.Machine.Translation
{
	public class AlignedWordPair
	{
		public AlignedWordPair(int sourceIndex, int targetIndex, double transProb, double alignProb)
		{
			SourceIndex = sourceIndex;
			TargetIndex = targetIndex;
			TranslationProbability = transProb;
			AlignmentProbability = alignProb;
		}

		public int SourceIndex { get; }
		public int TargetIndex { get; }
		public double TranslationProbability { get; }
		public double AlignmentProbability { get; }

		public override string ToString()
		{
			return $"{SourceIndex}-{TargetIndex}:{TranslationProbability:0.########}:{AlignmentProbability:0.########}";
		}
	}
}
