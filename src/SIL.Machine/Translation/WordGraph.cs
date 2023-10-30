using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Statistics;

namespace SIL.Machine.Translation
{
    public class WordGraph
    {
        private static readonly int[] EmptyArcIndices = new int[0];

        public const int InitialState = 0;

        private readonly HashSet<int> _finalStates;
        private readonly Dictionary<int, StateInfo> _states;

        public WordGraph(IEnumerable<string> sourceWords)
            : this(sourceWords, Enumerable.Empty<WordGraphArc>(), Enumerable.Empty<int>()) { }

        public WordGraph(
            IEnumerable<string> sourceTokens,
            IEnumerable<WordGraphArc> arcs,
            IEnumerable<int> finalStates,
            double initialStateScore = 0
        )
        {
            SourceTokens = sourceTokens.ToArray();
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

        public IReadOnlyList<string> SourceTokens { get; }
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

        public void ToGraphViz(TextWriter writer)
        {
            writer.WriteLine("digraph G {");

            for (int i = 0; i < StateCount; i++)
            {
                writer.Write(
                    "  {0} [shape=\"{1}\", color=\"{2}\"",
                    i,
                    i == 0 ? "diamond" : "circle",
                    i == 0
                        ? "green"
                        : FinalStates.Contains(i)
                            ? "red"
                            : "black"
                );
                if (FinalStates.Contains(i))
                    writer.Write(", peripheries=\"2\"");
                writer.WriteLine("];");
            }

            foreach (WordGraphArc arc in Arcs)
            {
                writer.Write(
                    "  {0} -> {1} [label=\"{2}",
                    arc.PrevState,
                    arc.NextState,
                    string.Join(" ", arc.TargetTokens).Replace("\"", "\\\"")
                );
                writer.WriteLine("\"];");
            }

            writer.WriteLine("}");
        }

        /// <summary>
        /// Removes redundant arcs from the word graph.
        /// TODO: This seems to affect the results of an interactive translation session, so don't use it yet.
        /// </summary>
        /// <returns>The optimized word graph.</returns>
        public WordGraph Optimize()
        {
            var dfaArcs = new List<WordGraphArc>();
            var dfaStates = new DfaStateCollection();
            var dfaFinalStates = new HashSet<int>();
            int nextDfaStateIndex = 1;
            var unmarkedStates = new Queue<DfaState>();

            unmarkedStates.Enqueue(new DfaState(0, new[] { new NfaState(0) }));

            while (unmarkedStates.Count > 0)
            {
                DfaState dfaState = unmarkedStates.Dequeue();
                var candidateArcs = new Dictionary<string, DfaArc>();
                foreach ((int arcIndex, NfaState nfaState) in GetArcIndices(dfaState))
                {
                    WordGraphArc arc = Arcs[arcIndex];
                    int nextWordIndex = nfaState.WordIndex + 1;
                    DfaArc candidateArc = candidateArcs.GetOrCreate(arc.TargetTokens[nextWordIndex]);
                    if (nextWordIndex == arc.TargetTokens.Count - 1)
                    {
                        candidateArc.NfaStates.Add(new NfaState(arc.NextState));

                        Path path;
                        if (dfaState.Paths.TryGetValue(nfaState.StateIndex, out Path prevPath))
                        {
                            path = new Path(
                                prevPath.StartState,
                                prevPath.Arcs.Concat(arcIndex),
                                LogSpace.Multiply(prevPath.Score, arc.Score)
                            );
                        }
                        else
                        {
                            path = new Path(dfaState.Index, new[] { arcIndex }, arc.Score);
                        }

                        if (
                            !candidateArc.Paths.TryGetValue(arc.NextState, out Path otherPath)
                            || path.Score > otherPath.Score
                        )
                        {
                            candidateArc.Paths[arc.NextState] = path;
                        }
                    }
                    else
                    {
                        candidateArc.NfaStates.Add(new NfaState(nfaState.StateIndex, arcIndex, nextWordIndex));
                        candidateArc.IsNextSubState = true;

                        if (dfaState.Paths.TryGetValue(nfaState.StateIndex, out Path prevPath))
                            candidateArc.Paths[nfaState.StateIndex] = prevPath;
                    }
                }

                foreach (DfaArc candidateArc in candidateArcs.Values)
                {
                    if (!dfaStates.TryGetValue(candidateArc.NfaStates, out DfaState nextDfaState))
                    {
                        int stateIndex = candidateArc.IsNextSubState ? dfaState.Index : nextDfaStateIndex++;
                        nextDfaState = new DfaState(stateIndex, candidateArc.NfaStates);
                        if (candidateArc.IsNextSubState)
                        {
                            foreach (KeyValuePair<int, Path> kvp in candidateArc.Paths)
                                nextDfaState.Paths.Add(kvp);
                        }
                        else
                        {
                            dfaStates.Add(nextDfaState);
                        }
                        unmarkedStates.Enqueue(nextDfaState);
                    }

                    bool isFinal = nextDfaState.NfaStates
                        .Where(s => !s.IsSubState)
                        .Any(s => FinalStates.Contains(s.StateIndex));
                    if ((isFinal || !candidateArc.IsNextSubState) && candidateArc.Paths.Count > 0)
                    {
                        Path bestPath = candidateArc.Paths.Values.MaxBy(p => p.Score);

                        int curState = bestPath.StartState;
                        for (int i = 0; i < bestPath.Arcs.Count; i++)
                        {
                            WordGraphArc nfaArc = Arcs[bestPath.Arcs[i]];
                            int nextState =
                                !candidateArc.IsNextSubState && i == bestPath.Arcs.Count - 1
                                    ? nextDfaState.Index
                                    : nextDfaStateIndex++;
                            dfaArcs.Add(
                                new WordGraphArc(
                                    curState,
                                    nextState,
                                    nfaArc.Score,
                                    nfaArc.TargetTokens,
                                    nfaArc.Alignment,
                                    nfaArc.SourceSegmentRange,
                                    nfaArc.Sources,
                                    nfaArc.Confidences
                                )
                            );
                            curState = nextState;
                        }
                        if (isFinal)
                            dfaFinalStates.Add(curState);
                    }
                }
            }

            return new WordGraph(SourceTokens, dfaArcs, dfaFinalStates, InitialStateScore);
        }

        private IEnumerable<(int ArcIndex, NfaState State)> GetArcIndices(DfaState dfaState)
        {
            foreach (NfaState nfaState in dfaState.NfaStates)
            {
                if (nfaState.IsSubState)
                {
                    yield return (nfaState.ArcIndex, nfaState);
                }
                else
                {
                    foreach (int arcIndex in GetNextArcIndices(nfaState.StateIndex))
                        yield return (arcIndex, nfaState);
                }
            }
        }

        private IEnumerable<WordGraphArc> GetBestPathFromFinalStateToState(int state)
        {
            double[] prevScores;
            int[] stateBestPredArcs;
            ComputePrevScores(state, out prevScores, out stateBestPredArcs);

            double bestFinalStateScore = LogSpace.Zero;
            int bestFinalState = InitialState;
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

        private class NfaState : IEquatable<NfaState>
        {
            public NfaState(int stateIndex, int arcIndex = -1, int wordIndex = -1)
            {
                StateIndex = stateIndex;
                ArcIndex = arcIndex;
                WordIndex = wordIndex;
            }

            public int StateIndex { get; }
            public int ArcIndex { get; }
            public int WordIndex { get; }
            public bool IsSubState => ArcIndex != -1;

            public override bool Equals(object obj)
            {
                return obj is NfaState state && Equals(state);
            }

            public bool Equals(NfaState other)
            {
                return other != null
                    && StateIndex == other.StateIndex
                    && ArcIndex == other.ArcIndex
                    && WordIndex == other.WordIndex;
            }

            public override int GetHashCode()
            {
                var hashCode = -1525131978;
                hashCode = hashCode * -1521134295 + StateIndex.GetHashCode();
                hashCode = hashCode * -1521134295 + ArcIndex.GetHashCode();
                hashCode = hashCode * -1521134295 + WordIndex.GetHashCode();
                return hashCode;
            }
        }

        private class DfaState
        {
            public DfaState(int index, IEnumerable<NfaState> nfaStates)
            {
                Index = index;
                NfaStates = new HashSet<NfaState>(nfaStates);
            }

            public int Index { get; }
            public ISet<NfaState> NfaStates { get; }
            public IDictionary<int, Path> Paths { get; } = new Dictionary<int, Path>();
        }

        private class Path
        {
            public Path(int startState, IEnumerable<int> arcs, double score)
            {
                StartState = startState;
                Arcs = arcs.ToArray();
                Score = score;
            }

            public int StartState { get; }
            public IList<int> Arcs { get; }
            public double Score { get; }
        }

        private class DfaArc
        {
            public ISet<NfaState> NfaStates { get; } = new HashSet<NfaState>();
            public IDictionary<int, Path> Paths { get; } = new Dictionary<int, Path>();
            public bool IsNextSubState { get; set; }
        }

        private class DfaStateCollection : KeyedCollection<ISet<NfaState>, DfaState>
        {
            public DfaStateCollection()
                : base(new SetEqualityComparer<NfaState>()) { }

            public bool TryGetValue(ISet<NfaState> key, out DfaState item)
            {
                if (Contains(key))
                {
                    item = this[key];
                    return true;
                }
                item = null;
                return false;
            }

            protected override ISet<NfaState> GetKeyForItem(DfaState item)
            {
                return item.NfaStates;
            }
        }

        private class SetEqualityComparer<T> : IEqualityComparer<ISet<T>>
        {
            public bool Equals(ISet<T> x, ISet<T> y)
            {
                if (x == null && y == null)
                    return true;
                else if (x == null || y == null)
                    return false;

                return x.SetEquals(y);
            }

            public int GetHashCode(ISet<T> obj)
            {
                return obj.Aggregate(0, (code, item) => code ^ item.GetHashCode());
            }
        }
    }
}
