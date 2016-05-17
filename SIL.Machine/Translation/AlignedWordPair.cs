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
		private readonly int _sourceIndex;
		private readonly int _targetIndex;
		private readonly double _confidence;
		private readonly TranslationSources _sources;

		public AlignedWordPair(int sourceIndex, int targetIndex, double confidence, TranslationSources sources)
		{
			_sourceIndex = sourceIndex;
			_targetIndex = targetIndex;
			_confidence = confidence;
			_sources = sources;
		}

		public int SourceIndex
		{
			get { return _sourceIndex; }
		}

		public int TargetIndex
		{
			get { return _targetIndex; }
		}

		public double Confidence
		{
			get { return _confidence; }
		}

		public TranslationSources Sources
		{
			get { return _sources; }
		}
	}
}
