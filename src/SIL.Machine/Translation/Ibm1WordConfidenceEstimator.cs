using System;
using System.Collections.Generic;
using SIL.Machine.Annotations;

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

        public void Estimate(WordGraph wordGraph)
        {
            var range = Range<int>.Create(0, wordGraph.SourceWords.Count);
            foreach (WordGraphArc arc in wordGraph.Arcs)
            {
                if (PhraseOnly)
                    range = arc.SourceSegmentRange;

                for (int k = 0; k < arc.Words.Count; k++)
                    arc.SetConfidence(k, GetConfidence(wordGraph.SourceWords, range, arc.Words[k]));
            }
        }

        public void Estimate(IReadOnlyList<string> sourceSegment, TranslationResult translationResult)
        {
            var range = Range<int>.Create(0, translationResult.SourceTokens.Count);
            int startIndex = 0;
            foreach (Phrase phrase in translationResult.Phrases)
            {
                if (PhraseOnly)
                    range = phrase.SourceSegmentRange;

                for (int j = startIndex; j < phrase.TargetSegmentCut; j++)
                {
                    double confidence = GetConfidence(
                        translationResult.SourceTokens,
                        range,
                        translationResult.TargetTokens[j]
                    );
                    translationResult.SetConfidence(j, confidence);
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
