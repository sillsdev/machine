using System;

namespace SIL.Machine.Translation
{
	[Flags]
	public enum TranslationSources
	{
		None = 0x0,
		Smt = 0x1,
		Transfer = 0x2
	}

	public class AlignedWordPair
	{
		public AlignedWordPair(int sourceIndex, int targetIndex, double confidence, TranslationSources sources)
		{
			SourceIndex = sourceIndex;
			TargetIndex = targetIndex;
			Confidence = confidence;
			Sources = sources;
		}

		public int SourceIndex { get; }

		public int TargetIndex { get; }

		public double Confidence { get; }

		public TranslationSources Sources { get; }
	}
}
