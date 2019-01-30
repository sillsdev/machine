using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Statistics;

namespace SIL.Machine.Translation
{
	public class WordGraph
	{
		private static readonly int[] EmptyArcIndices = new int[0];

		public const int InitialState = 0;

		private readonly HashSet<int> _finalStates;
		private readonly Dictionary<int, StateInfo> _states;

		public WordGraph()
			: this(Enumerable.Empty<WordGraphArc>(), Enumerable.Empty<int>())
		{
		}

		public WordGraph(IEnumerable<WordGraphArc> arcs, IEnumerable<int> finalStates, double initialStateScore = 0)
		{
			_states = new Dictionary<int, StateInfo>();
			var arcList = new List<WordGraphArc>();
			int maxState = -1;
			foreach (WordGraphArc arc in arcs)
			{
				if (arc.NextState > maxState)
					maxState = arc.NextState;
				if (arc.PrevState > maxState)
					maxState = arc.PrevState;

				int arcIndex = arcList.Count;
				GetOrCreateStateInfo(arc.PrevState).NextArcIndices.Add(arcIndex);
				GetOrCreateStateInfo(arc.NextState).PrevArcIndices.Add(arcIndex);
				arcList.Add(arc);
			}
			Arcs = arcList;
			StateCount = maxState + 1;
			_finalStates = new HashSet<int>(finalStates);
			InitialStateScore = initialStateScore;
		}

		public double InitialStateScore { get; }

		public IReadOnlyList<WordGraphArc> Arcs { get; }
		public int StateCount { get; }

		public ISet<int> FinalStates => _finalStates;

		public bool IsEmpty => Arcs.Count == 0;

		public IReadOnlyList<int> GetPrevArcIndices(int state)
		{
			StateInfo stateInfo;
			if (_states.TryGetValue(state, out stateInfo))
				return stateInfo.PrevArcIndices;
			return EmptyArcIndices;
		}

		public IReadOnlyList<int> GetNextArcIndices(int state)
		{
			StateInfo stateInfo;
			if (_states.TryGetValue(state, out stateInfo))
				return stateInfo.NextArcIndices;
			return EmptyArcIndices;
		}

		public IEnumerable<double> ComputeRestScores()
		{
			double[] restScores = Enumerable.Repeat(LogSpace.Zero, StateCount).ToArray();

			foreach (int state in _finalStates)
				restScores[state] = InitialStateScore;

			for (int i = Arcs.Count - 1; i >= 0; i--)
			{
				WordGraphArc arc = Arcs[i];
				double score = LogSpace.Multiply(arc.Score, restScores[arc.NextState]);
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

			prevScores = Enumerable.Repeat(LogSpace.Zero, StateCount).ToArray();
			stateBestPrevArcs = new int[StateCount];

			if (state == InitialState)
				prevScores[InitialState] = InitialStateScore;
			else
				prevScores[state] = 0;

			var accessibleStates = new HashSet<int> { state };
			for (int arcIndex = 0; arcIndex < Arcs.Count; arcIndex++)
			{
				WordGraphArc arc = Arcs[arcIndex];
				if (accessibleStates.Contains(arc.PrevState))
				{
					double score = LogSpace.Multiply(arc.Score, prevScores[arc.PrevState]);
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
						prevScores[arc.NextState] = LogSpace.Zero;
				}
			}
		}

		public IEnumerable<WordGraphArc> GetBestPathFromStateToFinalState(int state)
		{
			return GetBestPathFromFinalStateToState(state).Reverse();
		}

		private IEnumerable<WordGraphArc> GetBestPathFromFinalStateToState(int state)
		{
			double[] prevScores;
			int[] stateBestPredArcs;
			ComputePrevScores(state, out prevScores, out stateBestPredArcs);

			double bestFinalStateScore = LogSpace.Zero;
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

		private StateInfo GetOrCreateStateInfo(int state)
		{
			StateInfo stateInfo;
			if (!_states.TryGetValue(state, out stateInfo))
			{
				stateInfo = new StateInfo();
				_states[state] = stateInfo;
			}
			return stateInfo;
		}

		private class StateInfo
		{
			public List<int> PrevArcIndices { get; } = new List<int>();
			public List<int> NextArcIndices { get; } = new List<int>();
		}
	}
}
