﻿using System;
using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Statistics;

namespace SIL.Machine.Translation
{
    public class WppWordConfidenceEstimator : IWordConfidenceEstimator
    {
        private readonly WordGraphConfidences _wordGraphConfidences;

        public WppWordConfidenceEstimator(WordGraph wordGraph)
        {
            _wordGraphConfidences = ComputeWordGraphConfidences(wordGraph);
        }

        public double Estimate(Range<int> sourceSegmentRange, string targetWord)
        {
            return _wordGraphConfidences.GetConfidence(targetWord);
        }

        private WordGraphConfidences ComputeWordGraphConfidences(WordGraph wordGraph)
        {
            double normalizationFactor = LogSpace.Zero;
            double[] backwardProbs = new double[wordGraph.Arcs.Count];
            for (int i = wordGraph.Arcs.Count - 1; i >= 0; i--)
            {
                WordGraphArc arc = wordGraph.Arcs[i];
                double sum = LogSpace.One;
                if (!wordGraph.FinalStates.Contains(arc.NextState))
                {
                    sum = LogSpace.Zero;
                    foreach (int nextArcIndex in wordGraph.GetNextArcIndices(arc.NextState))
                    {
                        WordGraphArc nextArc = wordGraph.Arcs[nextArcIndex];
                        sum = LogSpace.Add(sum, LogSpace.Multiply(nextArc.Score, backwardProbs[nextArcIndex]));
                    }
                }
                backwardProbs[i] = sum;
                if (arc.PrevState == WordGraph.InitialState)
                    normalizationFactor = LogSpace.Add(
                        normalizationFactor,
                        LogSpace.Multiply(arc.Score, backwardProbs[i])
                    );
            }

            var rawWpps = new Dictionary<string, Dictionary<int, double>>();
            var forwardProbs = new (double Prob, int Index)[wordGraph.Arcs.Count];
            for (int i = 0; i < wordGraph.Arcs.Count; i++)
            {
                WordGraphArc arc = wordGraph.Arcs[i];
                double sum = LogSpace.One;
                int startIndex = 0;
                if (arc.PrevState != WordGraph.InitialState)
                {
                    sum = LogSpace.Zero;
                    foreach (int prevArcIndex in wordGraph.GetPrevArcIndices(arc.PrevState))
                    {
                        sum = LogSpace.Add(sum, forwardProbs[prevArcIndex].Prob);
                        startIndex = forwardProbs[prevArcIndex].Index;
                    }
                }
                forwardProbs[i] = (LogSpace.Multiply(arc.Score, sum), startIndex + arc.TargetTokens.Count);
                double prob = LogSpace.Multiply(forwardProbs[i].Prob, backwardProbs[i]);
                for (int j = 0; j < arc.TargetTokens.Count; j++)
                {
                    string word = arc.TargetTokens[j];
                    if (!rawWpps.TryGetValue(word, out Dictionary<int, double> indexWpps))
                    {
                        indexWpps = new Dictionary<int, double>();
                        rawWpps[word] = indexWpps;
                    }
                    int index = startIndex + j;
                    if (!indexWpps.TryGetValue(index, out double rawWpp))
                        rawWpp = LogSpace.Zero;
                    rawWpp = LogSpace.Add(rawWpp, prob);
                    indexWpps[index] = rawWpp;
                }
            }

            return new WordGraphConfidences(rawWpps, normalizationFactor);
        }

        private class WordGraphConfidences
        {
            private readonly Dictionary<string, Dictionary<int, double>> _rawWpps;
            private readonly double _normalizationFactor;
            private readonly Dictionary<string, double> _cachedConfidences;

            public WordGraphConfidences(
                Dictionary<string, Dictionary<int, double>> rawWpProbs,
                double normalizationFactor
            )
            {
                _rawWpps = rawWpProbs;
                _normalizationFactor = normalizationFactor;
                _cachedConfidences = new Dictionary<string, double>();
            }

            public double GetConfidence(string targetWord)
            {
                if (_cachedConfidences.TryGetValue(targetWord, out double confidence))
                    return confidence;

                confidence = 0;
                if (_rawWpps.TryGetValue(targetWord, out Dictionary<int, double> indexWpps))
                {
                    double maxWpp = 0;
                    foreach (double rawWpp in indexWpps.Values)
                    {
                        double logWpp = LogSpace.Divide(rawWpp, _normalizationFactor);
                        double wpp;
                        if (logWpp > LogSpace.One)
                            wpp = 1.0;
                        else if (logWpp < -20)
                            wpp = 0;
                        else
                            wpp = LogSpace.ToStandardSpace(logWpp);
                        maxWpp = Math.Max(maxWpp, wpp);
                    }
                    confidence = maxWpp;
                }

                _cachedConfidences[targetWord] = confidence;
                return confidence;
            }
        }
    }
}
