using SIL.Machine.Statistics;
using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public class WppWordConfidenceEstimator : IWordConfidenceEstimator
	{
		private readonly ISmtEngine _smtEngine;

		public WppWordConfidenceEstimator(ISmtEngine smtEngine)
		{
			_smtEngine = smtEngine;
		}

		public IWordConfidences Estimate(IReadOnlyList<string> sourceSegment, WordGraph wordGraph = null)
		{
			if (wordGraph == null)
				wordGraph = _smtEngine.GetWordGraph(sourceSegment);

			double normalizationFactor = LogSpace.Zero;
			var backwardProbs = new double[wordGraph.Arcs.Count];
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
						sum = LogSpace.Add(sum, LogSpace.Multiple(nextArc.Score, backwardProbs[nextArcIndex]));
					}
				}
				backwardProbs[i] = sum;
				if (arc.PrevState == WordGraph.InitialState)
				{
					normalizationFactor = LogSpace.Add(normalizationFactor,
						LogSpace.Multiple(arc.Score, backwardProbs[i]));
				}
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
				forwardProbs[i] = (LogSpace.Multiple(arc.Score, sum), startIndex + arc.Words.Count);
				double prob = LogSpace.Multiple(forwardProbs[i].Prob, backwardProbs[i]);
				for (int j = 0; j < arc.Words.Count; j++)
				{
					string word = arc.Words[j];
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

			return new WordConfidences(rawWpps, normalizationFactor);
		}

		private class WordConfidences : IWordConfidences
		{
			private readonly Dictionary<string, Dictionary<int, double>> _rawWpps;
			private readonly double _normalizationFactor;

			public WordConfidences(Dictionary<string, Dictionary<int, double>> rawWpps, double normalizationFactor)
			{
				_rawWpps = rawWpps;
				_normalizationFactor = normalizationFactor;
			}

			public double GetConfidence(string targetWord)
			{
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
					return maxWpp;
				}
				return 0;
			}
		}
	}
}
