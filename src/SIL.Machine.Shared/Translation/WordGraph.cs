using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class WordGraph
	{
		public const int InitialState = 0;

		private const double SmallScore = -999999999;

		private readonly HashSet<int> _finalStates;

		public WordGraph(IEnumerable<WordGraphArc> arcs, IEnumerable<int> finalStates, double initialStateScore = 0)
		{
			Arcs = arcs.ToArray();
			int maxState = -1;
			foreach (WordGraphArc arc in Arcs)
			{
				if (arc.NextState > maxState)
					maxState = arc.NextState;
				if (arc.PrevState > maxState)
					maxState = arc.PrevState;
			}
			StateCount = maxState + 1;
			_finalStates = new HashSet<int>(finalStates);
			InitialStateScore = initialStateScore;
		}

		public double InitialStateScore { get; }

		public IReadOnlyList<WordGraphArc> Arcs { get; }
		public int StateCount { get; }

		public IEnumerable<int> FinalStates => _finalStates;

		public bool IsEmpty => Arcs.Count == 0;

		public IEnumerable<double> ComputeRestScores()
		{
			double[] restScores = Enumerable.Repeat(SmallScore, StateCount).ToArray();

			foreach (int state in _finalStates)
				restScores[state] = InitialStateScore;

			for (int i = Arcs.Count - 1; i >= 0; i--)
			{
				WordGraphArc arc = Arcs[i];

				double score = arc.Score + restScores[arc.NextState];
				if (score < SmallScore)
					score = SmallScore;
				if (score > restScores[arc.PrevState])
					restScores[arc.PrevState] = score;
			}

			return restScores;
		}

		private void ComputePrevScores(int state, out double[] prevScores, out int[] stateBestPrevArcs)
		{
			if (IsEmpty)
			{
				prevScores = new double[0];
				stateBestPrevArcs = new int[0];
				return;
			}

			prevScores = Enumerable.Repeat(SmallScore, StateCount).ToArray();
			stateBestPrevArcs = new int[StateCount];

			if (state == InitialState)
				prevScores[InitialState] = InitialStateScore;
			else
				prevScores[state] = 0;

			var accessibleStates = new HashSet<int> {state};
			for (int arcIndex = 0; arcIndex < Arcs.Count; arcIndex++)
			{
				WordGraphArc arc = Arcs[arcIndex];

				if (accessibleStates.Contains(arc.PrevState))
				{
					double score = arc.Score + prevScores[arc.PrevState];
					if (score < SmallScore)
						score = SmallScore;
					if (score > prevScores[arc.NextState])
					{
						prevScores[arc.NextState] = score;
						stateBestPrevArcs[arc.NextState] = arcIndex;
					}
					accessibleStates.Add(arc.NextState);
				}
				else
				{
					if (!accessibleStates.Contains(arc.NextState))
						prevScores[arc.NextState] = SmallScore;
				}
			}
		}

		public IEnumerable<WordGraphArc> GetBestPathFromFinalStateToState(int state)
		{
			double[] prevScores;
			int[] stateBestPredArcs;
			ComputePrevScores(state, out prevScores, out stateBestPredArcs);

			double bestFinalStateScore = SmallScore;
			int bestFinalState = 0;
			foreach (int finalState in _finalStates)
			{
				double score = prevScores[finalState];
				if (bestFinalStateScore < score)
				{
					bestFinalState = finalState;
					bestFinalStateScore = score;
				}
			}

			if (!_finalStates.Contains(bestFinalState))
				yield break;

			int curState = bestFinalState;
			bool end = false;
			while (!end)
			{
				if (curState == state)
				{
					end = true;
				}
				else
				{
					int arcIndex = stateBestPredArcs[curState];
					WordGraphArc arc = Arcs[arcIndex];
					yield return arc;
					curState = arc.PrevState;
				}
			}
		}
	}
}
