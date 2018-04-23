using SIL.Machine.Annotations;
using System;
using System.Collections.Generic;
using static SIL.Machine.Translation.TranslationResultBuilder;

namespace SIL.Machine.Translation
{
	public class Ibm1WordConfidenceEstimator : IWordConfidenceEstimator
	{
		private readonly Func<string, string, double> _getTranslationProb;

		public Ibm1WordConfidenceEstimator(Func<string, string, double> getTranslationProb)
		{
			_getTranslationProb = getTranslationProb;
		}

		public bool PhraseOnly { get; set; } = true;

		public void Estimate(IReadOnlyList<string> sourceSegment, WordGraph wordGraph)
		{
			var range = Range<int>.Create(0, sourceSegment.Count);
			foreach (WordGraphArc arc in wordGraph.Arcs)
			{
				if (PhraseOnly)
					range = arc.SourceSegmentRange;

				for (int k = 0; k < arc.Words.Count; k++)
					arc.WordConfidences[k] = GetConfidence(sourceSegment, range, arc.Words[k]);
			}
		}

		public void Estimate(IReadOnlyList<string> sourceSegment, TranslationResultBuilder builder)
		{
			var range = Range<int>.Create(0, sourceSegment.Count);
			int startIndex = 0;
			foreach (PhraseInfo phrase in builder.Phrases)
			{
				if (PhraseOnly)
					range = phrase.SourceSegmentRange;

				for (int j = startIndex; j < phrase.TargetCut; j++)
				{
					double confidence = GetConfidence(sourceSegment, range, builder.Words[j]);
					builder.SetConfidence(j, confidence);
				}
			}
		}

		private double GetConfidence(IReadOnlyList<string> sourceSegment, Range<int> range, string targetWord)
		{
			double maxConfidence = _getTranslationProb(null, targetWord);
			for (int i = range.Start; i < range.End; i++)
			{
				double confidence = _getTranslationProb(sourceSegment[i], targetWord);
				if (confidence > maxConfidence)
					maxConfidence = confidence;
			}

			return maxConfidence;
		}
	}
}
