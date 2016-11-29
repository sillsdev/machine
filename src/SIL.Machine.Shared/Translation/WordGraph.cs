using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class WordGraph
	{
		private const double SmallScore = -999999999;

		private readonly List<WordGraphArc> _arcs;
		private readonly ReadOnlyCollection<WordGraphArc> _readOnlyArcs;
		private readonly List<State> _states;
		private readonly List<double[]> _arcScoreComps;
		private readonly List<Tuple<string, double>> _compWeights;
		private readonly HashSet<int> _finalStateIndices;

		public WordGraph()
		{
			_arcs = new List<WordGraphArc>();
			_readOnlyArcs = new ReadOnlyCollection<WordGraphArc>(_arcs);
			_states = new List<State>();
			_arcScoreComps = new List<double[]>();
			_compWeights = new List<Tuple<string, double>>();
			_finalStateIndices = new HashSet<int>();
		}

		public WordGraph(string wordGraphStr, double initialStateScore = 0)
			: this()
		{
			Load(wordGraphStr);
			InitialStateScore = initialStateScore;
		}

		public double InitialStateScore { get; set; }

		public IReadOnlyList<WordGraphArc> Arcs => _readOnlyArcs;
		public int StateCount => _states.Count;

		public bool IsEmpty => _arcs.Count == 0;

		public void AddArc(WordGraphArc arc, IEnumerable<double> scoreComponents)
		{
			_arcs.Add(arc);
			_arcScoreComps.Add(scoreComponents.ToArray());
			int arcIndex = _arcs.Count - 1;

			if (arc.SuccStateIndex >= _states.Count)
			{
				// there were not any arcs to the successor state, so create state
				while (arc.SuccStateIndex >= _states.Count)
					_states.Add(new State());
			}
			_states[arc.PredStateIndex].SuccArcs.Add(arcIndex);
			_states[arc.SuccStateIndex].PredArcs.Add(arcIndex);
		}

		public void Load(string wordGraphStr)
		{
			Clear();

			if (string.IsNullOrEmpty(wordGraphStr))
				return;

			string[] lines = wordGraphStr.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
			int i = 0;
			if (lines[i].StartsWith("#"))
			{
				string[] weights = Split(lines[i]);
				for (int j = 1; j < weights.Length; j += 3)
					_compWeights.Add(Tuple.Create(weights[j], double.Parse(weights[j + 1], CultureInfo.InvariantCulture)));
				i++;
			}

			string[] finalStates = Split(lines[i]);
			_finalStateIndices.UnionWith(finalStates.Select(index => int.Parse(index)));
			i++;

			for (; i < lines.Length; i++)
			{
				string[] arcParts = Split(lines[i]);

				if (arcParts.Length < 3)
					continue;

				int predStateIndex = int.Parse(arcParts[0]);
				int succStateIndex = int.Parse(arcParts[1]);
				double score = double.Parse(arcParts[2], CultureInfo.InvariantCulture);
				int srcStartIndex = 0;
				if (arcParts.Length >= 4)
					srcStartIndex = int.Parse(arcParts[3]);
				int srcEndIndex = 0;
				if (arcParts.Length >= 5)
					srcEndIndex = int.Parse(arcParts[4]);
				bool unknown = false;
				if (arcParts.Length >= 6)
					unknown = arcParts[5] == "1";

				int j = 6;
				var scrComps = new List<double>();
				if (arcParts.Length >= 7)
				{
					if (arcParts[j] == "|||")
					{
						j++;
						while (j < arcParts.Length && arcParts[j] != "|||")
						{
							scrComps.Add(double.Parse(arcParts[j], CultureInfo.InvariantCulture));
							j++;
						}
						j++;
					}
				}

				var words = new List<string>();
				for (; j < arcParts.Length; j++)
					words.Add(arcParts[j]);

				var arc = new WordGraphArc(predStateIndex, succStateIndex, score, words, srcStartIndex, srcEndIndex, unknown);
				AddArc(arc, scrComps);
			}
		}

		private static string[] Split(string line)
		{
			return line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
		}

		public IEnumerable<double> ComputeRestScores()
		{
			double[] restScores = Enumerable.Repeat(SmallScore, _states.Count).ToArray();

			foreach (int stateIndex in _finalStateIndices)
				restScores[stateIndex] = InitialStateScore;

			for (int i = _arcs.Count - 1; i >= 0; i--)
			{
				WordGraphArc arc = _arcs[i];

				double score = arc.Score + restScores[arc.SuccStateIndex];
				if (score < SmallScore)
					score = SmallScore;
				if (score > restScores[arc.PredStateIndex])
					restScores[arc.PredStateIndex] = score;
			}

			return restScores;
		}

		private void ComputePrevScores(int stateIndex, out double[] prevScores, out int[] stateBestPredArcs)
		{
			if (IsEmpty)
			{
				prevScores = new double[0];
				stateBestPredArcs = new int[0];
				return;
			}

			prevScores = Enumerable.Repeat(SmallScore, _states.Count).ToArray();
			stateBestPredArcs = new int[_states.Count];

			if (stateIndex == 0)
				prevScores[0] = InitialStateScore;
			else
				prevScores[stateIndex] = 0;

			var accessibleStates = new HashSet<int> {stateIndex};
			for (int arcIndex = 0; arcIndex < _arcs.Count; arcIndex++)
			{
				WordGraphArc arc = _arcs[arcIndex];

				if (accessibleStates.Contains(arc.PredStateIndex))
				{
					double score = arc.Score + prevScores[arc.PredStateIndex];
					if (score < SmallScore)
						score = SmallScore;
					if (score > prevScores[arc.SuccStateIndex])
					{
						prevScores[arc.SuccStateIndex] = score;
						stateBestPredArcs[arc.SuccStateIndex] = arcIndex;
					}
					accessibleStates.Add(arc.SuccStateIndex);
				}
				else
				{
					if (!accessibleStates.Contains(arc.SuccStateIndex))
						prevScores[arc.SuccStateIndex] = SmallScore;
				}
			}
		}

		public IEnumerable<WordGraphArc> GetBestPathFromFinalStateToState(int stateIndex)
		{
			double[] prevScores;
			int[] stateBestPredArcs;
			ComputePrevScores(stateIndex, out prevScores, out stateBestPredArcs);

			double bestFinalStateScore = SmallScore;
			int bestFinalStateIndex = 0;
			foreach (int finalStateIndex in _finalStateIndices)
			{
				double score = prevScores[finalStateIndex];
				if (bestFinalStateScore < score)
				{
					bestFinalStateIndex = finalStateIndex;
					bestFinalStateScore = score;
				}
			}

			if (!_finalStateIndices.Contains(bestFinalStateIndex))
				yield break;

			int curStateIndex = bestFinalStateIndex;
			bool end = false;
			while (!end)
			{
				if (curStateIndex == stateIndex)
				{
					end = true;
				}
				else
				{
					int arcIndex = stateBestPredArcs[curStateIndex];
					WordGraphArc arc = _arcs[arcIndex];
					yield return arc;
					curStateIndex = arc.PredStateIndex;
				}
			}
		}

		public void Clear()
		{
			_arcs.Clear();
			_states.Clear();
			_arcScoreComps.Clear();
			_compWeights.Clear();
			_finalStateIndices.Clear();
		}

		private class State
		{
			public IList<int> PredArcs { get; } = new List<int>();
			public IList<int> SuccArcs { get; } = new List<int>();
		}
	}
}
