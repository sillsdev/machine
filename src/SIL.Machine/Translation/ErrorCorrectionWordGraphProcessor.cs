﻿using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.DataStructures;
using SIL.Machine.Statistics;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Translation
{
    public class ErrorCorrectionWordGraphProcessor
    {
        private readonly WordGraph _wordGraph;
        private readonly double[] _restScores;
        private readonly ErrorCorrectionModel _ecm;
        private readonly IDetokenizer<string, string> _targetDetokenizer;
        private readonly List<EcmScoreInfo> _stateEcmScoreInfos;
        private readonly List<List<EcmScoreInfo>> _arcEcmScoreInfos;
        private readonly List<List<double>> _stateBestScores;
        private readonly List<double> _stateWordGraphScores;
        private readonly List<List<int>> _stateBestPrevArcs;
        private readonly HashSet<int> _statesInvolvedInArcs;
        private string[] _prevPrefix;
        private bool _prevIsLastWordComplete;

        public ErrorCorrectionWordGraphProcessor(
            ErrorCorrectionModel ecm,
            IDetokenizer<string, string> targetDetokenizer,
            WordGraph wordGraph,
            double ecmWeight = 1,
            double wordGraphWeight = 1
        )
        {
            _ecm = ecm;
            _targetDetokenizer = targetDetokenizer;
            _wordGraph = wordGraph;
            EcmWeight = ecmWeight;
            WordGraphWeight = wordGraphWeight;

            _restScores = _wordGraph.ComputeRestScores().ToArray();
            _stateEcmScoreInfos = new List<EcmScoreInfo>();
            _arcEcmScoreInfos = new List<List<EcmScoreInfo>>();
            _stateBestScores = new List<List<double>>();
            _stateWordGraphScores = new List<double>();
            _stateBestPrevArcs = new List<List<int>>();
            _statesInvolvedInArcs = new HashSet<int>();
            _prevPrefix = new string[0];

            InitStates();
            InitArcs();
        }

        public double ConfidenceThreshold { get; set; }

        private void InitStates()
        {
            for (int i = 0; i < _wordGraph.StateCount; i++)
            {
                _stateEcmScoreInfos.Add(new EcmScoreInfo());
                _stateWordGraphScores.Add(LogSpace.Zero);
                _stateBestScores.Add(new List<double>());
                _stateBestPrevArcs.Add(new List<int>());
            }

            if (!_wordGraph.IsEmpty)
            {
                _ecm.SetupInitialEsi(_stateEcmScoreInfos[WordGraph.InitialState]);
                UpdateInitialStateBestScores();
            }
        }

        private void InitArcs()
        {
            for (int arcIndex = 0; arcIndex < _wordGraph.Arcs.Count; arcIndex++)
            {
                WordGraphArc arc = _wordGraph.Arcs[arcIndex];

                // init ecm score info for each word of arc
                EcmScoreInfo prevEsi = _stateEcmScoreInfos[arc.PrevState];
                var esis = new List<EcmScoreInfo>();
                foreach (string word in arc.TargetTokens)
                {
                    var esi = new EcmScoreInfo();
                    _ecm.SetupEsi(esi, prevEsi, word);
                    esis.Add(esi);
                    prevEsi = esi;
                }
                _arcEcmScoreInfos.Add(esis);

                // init best scores for the arc's successive state
                UpdateStateBestScores(arcIndex, 0);

                _statesInvolvedInArcs.Add(arc.PrevState);
                _statesInvolvedInArcs.Add(arc.NextState);
            }
        }

        private void UpdateInitialStateBestScores()
        {
            EcmScoreInfo esi = _stateEcmScoreInfos[WordGraph.InitialState];

            _stateWordGraphScores[WordGraph.InitialState] = _wordGraph.InitialStateScore;

            List<double> bestScores = _stateBestScores[WordGraph.InitialState];
            List<int> bestPrevArcs = _stateBestPrevArcs[WordGraph.InitialState];

            bestScores.Clear();
            bestPrevArcs.Clear();

            foreach (double score in esi.Scores)
            {
                bestScores.Add((EcmWeight * -score) + (WordGraphWeight * _wordGraph.InitialStateScore));
                bestPrevArcs.Add(int.MaxValue);
            }
        }

        private void UpdateStateBestScores(int arcIndex, int prefixDiffSize)
        {
            WordGraphArc arc = _wordGraph.Arcs[arcIndex];
            List<EcmScoreInfo> arcEsis = _arcEcmScoreInfos[arcIndex];

            EcmScoreInfo prevEsi = arcEsis.Count == 0 ? _stateEcmScoreInfos[arc.PrevState] : arcEsis[arcEsis.Count - 1];

            double wordGraphScore = _stateWordGraphScores[arc.PrevState] + arc.Score;

            List<double> nextStateBestScores = _stateBestScores[arc.NextState];
            List<int> nextStateBestPrevArcs = _stateBestPrevArcs[arc.NextState];

            var positions = new List<int>();
            int startPos = prefixDiffSize == 0 ? 0 : prevEsi.Scores.Count - prefixDiffSize;
            for (int i = startPos; i < prevEsi.Scores.Count; i++)
            {
                double newScore = (EcmWeight * -prevEsi.Scores[i]) + (WordGraphWeight * wordGraphScore);

                if (i == nextStateBestScores.Count || nextStateBestScores[i] < newScore)
                {
                    AddOrReplace(nextStateBestScores, i, newScore);
                    positions.Add(i);
                    AddOrReplace(nextStateBestPrevArcs, i, arcIndex);
                }
            }

            _stateEcmScoreInfos[arc.NextState].UpdatePositions(prevEsi, positions);

            if (wordGraphScore > _stateWordGraphScores[arc.NextState])
                _stateWordGraphScores[arc.NextState] = wordGraphScore;
        }

        private void AddOrReplace<T>(List<T> list, int index, T item)
        {
            if (index > list.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index == list.Count)
                list.Add(item);
            else
                list[index] = item;
        }

        public double EcmWeight { get; }
        public double WordGraphWeight { get; }

        public void Correct(IReadOnlyList<string> prefix, bool isLastWordComplete)
        {
            // get valid portion of the processed prefix vector
            int validProcPrefixCount = 0;
            for (int i = 0; i < _prevPrefix.Length; i++)
            {
                if (i >= prefix.Count)
                    break;

                if (i == _prevPrefix.Length - 1 && i == prefix.Count - 1)
                {
                    if (_prevPrefix[i] == prefix[i] && _prevIsLastWordComplete == isLastWordComplete)
                        validProcPrefixCount++;
                }
                else if (_prevPrefix[i] == prefix[i])
                {
                    validProcPrefixCount++;
                }
            }

            int diffSize = _prevPrefix.Length - validProcPrefixCount;
            if (diffSize > 0)
            {
                // adjust size of info for arcs
                foreach (List<EcmScoreInfo> esis in _arcEcmScoreInfos)
                {
                    foreach (EcmScoreInfo esi in esis)
                    {
                        for (int i = 0; i < diffSize; i++)
                            esi.RemoveLast();
                    }
                }

                // adjust size of info for states
                foreach (int state in _statesInvolvedInArcs)
                {
                    for (int i = 0; i < diffSize; i++)
                    {
                        _stateEcmScoreInfos[state].RemoveLast();
                        _stateBestScores[state].RemoveAt(_stateBestScores[state].Count - 1);
                        _stateBestPrevArcs[state].RemoveAt(_stateBestPrevArcs[state].Count - 1);
                    }
                }
            }

            // get difference between prefix and valid portion of processed prefix
            var prefixDiff = new string[prefix.Count - validProcPrefixCount];
            for (int i = 0; i < prefixDiff.Length; i++)
                prefixDiff[i] = prefix[validProcPrefixCount + i];

            // process word-graph given prefix difference
            ProcessWordGraphForPrefixDiff(prefixDiff, isLastWordComplete);

            _prevPrefix = prefix.ToArray();
            _prevIsLastWordComplete = isLastWordComplete;
        }

        public IEnumerable<TranslationResult> GetResults()
        {
            PriorityQueue<Hypothesis> queue = GetHypotheses();

            foreach (Hypothesis hypothesis in Search(queue))
            {
                var builder = new TranslationResultBuilder(_wordGraph.SourceTokens)
                {
                    TargetDetokenizer = _targetDetokenizer
                };
                BuildCorrectionFromHypothesis(builder, _prevPrefix, _prevIsLastWordComplete, hypothesis);
                yield return builder.ToResult();
            }
        }

        private IEnumerable<Hypothesis> Search(PriorityQueue<Hypothesis> queue)
        {
            while (!queue.IsEmpty)
            {
                Hypothesis hypothesis = queue.Dequeue();
                int lastState =
                    hypothesis.Arcs.Count == 0
                        ? hypothesis.StartState
                        : hypothesis.Arcs[hypothesis.Arcs.Count - 1].NextState;

                if (_wordGraph.FinalStates.Contains(lastState))
                {
                    yield return hypothesis;
                }
                else if (ConfidenceThreshold <= 0)
                {
                    hypothesis.Arcs.AddRange(_wordGraph.GetBestPathFromStateToFinalState(lastState));
                    yield return hypothesis;
                }
                else
                {
                    double score = hypothesis.Score - (WordGraphWeight * _restScores[lastState]);
                    IReadOnlyList<int> arcIndices = _wordGraph.GetNextArcIndices(lastState);
                    bool enqueuedArc = false;
                    for (int i = 0; i < arcIndices.Count; i++)
                    {
                        int arcIndex = arcIndices[i];
                        WordGraphArc arc = _wordGraph.Arcs[arcIndex];
                        if (IsArcPruned(arc))
                            continue;

                        Hypothesis newHypothesis = hypothesis;
                        if (i < arcIndices.Count - 1)
                            newHypothesis = newHypothesis.Clone();
                        newHypothesis.Score = score;
                        newHypothesis.Score += arc.Score;
                        newHypothesis.Score += _restScores[arc.NextState];
                        newHypothesis.Arcs.Add(arc);
                        queue.Enqueue(newHypothesis);
                        enqueuedArc = true;
                    }

                    if (!enqueuedArc && (hypothesis.StartArcIndex != -1 || hypothesis.Arcs.Count > 0))
                    {
                        hypothesis.Arcs.AddRange(_wordGraph.GetBestPathFromStateToFinalState(lastState));
                        yield return hypothesis;
                    }
                }
            }
        }

        private void ProcessWordGraphForPrefixDiff(string[] prefixDiff, bool isLastWordComplete)
        {
            if (prefixDiff.Length == 0)
                return;

            if (!_wordGraph.IsEmpty)
            {
                EcmScoreInfo prevInitialEsi = _stateEcmScoreInfos[WordGraph.InitialState];
                _ecm.ExtendInitialEsi(_stateEcmScoreInfos[WordGraph.InitialState], prevInitialEsi, prefixDiff);
                UpdateInitialStateBestScores();
            }

            for (int arcIndex = 0; arcIndex < _wordGraph.Arcs.Count; arcIndex++)
            {
                WordGraphArc arc = _wordGraph.Arcs[arcIndex];

                // update ecm score info for each word of arc
                EcmScoreInfo prevEsi = _stateEcmScoreInfos[arc.PrevState];
                List<EcmScoreInfo> esis = _arcEcmScoreInfos[arcIndex];
                while (esis.Count < arc.TargetTokens.Count)
                    esis.Add(new EcmScoreInfo());
                for (int i = 0; i < arc.TargetTokens.Count; i++)
                {
                    EcmScoreInfo esi = esis[i];
                    _ecm.ExtendEsi(
                        esi,
                        prevEsi,
                        arc.IsUnknown ? string.Empty : arc.TargetTokens[i],
                        prefixDiff,
                        isLastWordComplete
                    );
                    prevEsi = esi;
                }

                // update best scores for the arc's successive state
                UpdateStateBestScores(arcIndex, prefixDiff.Length);
            }
        }

        private PriorityQueue<Hypothesis> GetHypotheses()
        {
            var queue = new PriorityQueue<Hypothesis>(1000);

            // add hypotheses starting before each word in each arc
            for (int arcIndex = 0; arcIndex < _wordGraph.Arcs.Count; arcIndex++)
            {
                WordGraphArc arc = _wordGraph.Arcs[arcIndex];
                if (!IsArcPruned(arc))
                {
                    double wordGraphScore = _stateWordGraphScores[arc.PrevState] + arc.Score;

                    for (int i = -1; i < arc.TargetTokens.Count - 1; i++)
                    {
                        EcmScoreInfo esi =
                            i == -1 ? _stateEcmScoreInfos[arc.PrevState] : _arcEcmScoreInfos[arcIndex][i];
                        double score =
                            (WordGraphWeight * wordGraphScore)
                            + (EcmWeight * -esi.Scores[esi.Scores.Count - 1])
                            + (WordGraphWeight * _restScores[arc.NextState]);
                        queue.Enqueue(new Hypothesis(score, arc.NextState, arcIndex, i));
                    }
                }
            }

            // add hypotheses starting before each final state
            foreach (int state in _wordGraph.FinalStates)
            {
                double restScore = _restScores[state];
                List<double> bestScores = _stateBestScores[state];

                double score = bestScores[bestScores.Count - 1] + (WordGraphWeight * restScore);
                queue.Enqueue(new Hypothesis(score, state));
            }

            return queue;
        }

        private bool IsArcPruned(WordGraphArc arc)
        {
            return !arc.IsUnknown && arc.Confidences.Any(c => c < ConfidenceThreshold);
        }

        private void BuildCorrectionFromHypothesis(
            TranslationResultBuilder builder,
            string[] prefix,
            bool isLastWordComplete,
            Hypothesis hypothesis
        )
        {
            int uncorrectedPrefixLen;
            if (hypothesis.StartArcIndex == -1)
            {
                AddBestUncorrectedPrefixState(builder, prefix.Length, hypothesis.StartState);
                uncorrectedPrefixLen = builder.TargetTokens.Count;
            }
            else
            {
                AddBestUncorrectedPrefixSubState(
                    builder,
                    prefix.Length,
                    hypothesis.StartArcIndex,
                    hypothesis.StartArcWordIndex
                );
                WordGraphArc firstArc = _wordGraph.Arcs[hypothesis.StartArcIndex];
                uncorrectedPrefixLen =
                    builder.TargetTokens.Count - (firstArc.TargetTokens.Count - hypothesis.StartArcWordIndex) + 1;
            }

            int alignmentColsToAddCount = _ecm.CorrectPrefix(builder, uncorrectedPrefixLen, prefix, isLastWordComplete);

            foreach (WordGraphArc arc in hypothesis.Arcs)
            {
                UpdateCorrectionFromArc(builder, arc, alignmentColsToAddCount);
                alignmentColsToAddCount = 0;
            }
        }

        private void AddBestUncorrectedPrefixState(TranslationResultBuilder builder, int procPrefixPos, int state)
        {
            var arcs = new Stack<WordGraphArc>();

            int curState = state;
            int curProcPrefixPos = procPrefixPos;
            while (curState != WordGraph.InitialState)
            {
                int arcIndex = _stateBestPrevArcs[curState][curProcPrefixPos];
                WordGraphArc arc = _wordGraph.Arcs[arcIndex];

                for (int i = arc.TargetTokens.Count - 1; i >= 0; i--)
                {
                    IReadOnlyList<int> predPrefixWords = _arcEcmScoreInfos[arcIndex][i].GetLastInsPrefixWordFromEsi();
                    curProcPrefixPos = predPrefixWords[curProcPrefixPos];
                }

                arcs.Push(arc);

                curState = arc.PrevState;
            }

            foreach (WordGraphArc arc in arcs)
                UpdateCorrectionFromArc(builder, arc, 0);
        }

        private void AddBestUncorrectedPrefixSubState(
            TranslationResultBuilder builder,
            int procPrefixPos,
            int arcIndex,
            int arcWordIndex
        )
        {
            WordGraphArc arc = _wordGraph.Arcs[arcIndex];

            int curProcPrefixPos = procPrefixPos;
            for (int i = arcWordIndex; i >= 0; i--)
            {
                IReadOnlyList<int> predPrefixWords = _arcEcmScoreInfos[arcIndex][i].GetLastInsPrefixWordFromEsi();
                curProcPrefixPos = predPrefixWords[curProcPrefixPos];
            }

            AddBestUncorrectedPrefixState(builder, curProcPrefixPos, arc.PrevState);

            UpdateCorrectionFromArc(builder, arc, 0);
        }

        private void UpdateCorrectionFromArc(
            TranslationResultBuilder builder,
            WordGraphArc arc,
            int alignmentColsToAddCount
        )
        {
            for (int i = 0; i < arc.TargetTokens.Count; i++)
                builder.AppendToken(arc.TargetTokens[i], arc.Sources[i], arc.Confidences[i]);

            WordAlignmentMatrix alignment = arc.Alignment;
            if (alignmentColsToAddCount > 0)
            {
                var newAlignment = new WordAlignmentMatrix(
                    alignment.RowCount,
                    alignment.ColumnCount + alignmentColsToAddCount
                );
                for (int j = 0; j < alignment.ColumnCount; j++)
                {
                    for (int i = 0; i < alignment.RowCount; i++)
                        newAlignment[i, alignmentColsToAddCount + j] = alignment[i, j];
                }
                alignment = newAlignment;
            }

            builder.MarkPhrase(arc.SourceSegmentRange, alignment);
        }

        private class Hypothesis : PriorityQueueNodeBase, IComparable<Hypothesis>
        {
            public Hypothesis(double score, int startState, int startArcIndex = -1, int startArcWordIndex = -1)
            {
                Score = score;
                StartState = startState;
                StartArcIndex = startArcIndex;
                StartArcWordIndex = startArcWordIndex;
                Arcs = new List<WordGraphArc>();
            }

            public Hypothesis(Hypothesis other)
            {
                Score = other.Score;
                StartState = other.StartState;
                StartArcIndex = other.StartArcIndex;
                StartArcWordIndex = other.StartArcWordIndex;
                Arcs = other.Arcs.ToList();
            }

            public double Score { get; set; }
            public int StartState { get; }
            public int StartArcIndex { get; }
            public int StartArcWordIndex { get; }
            public List<WordGraphArc> Arcs { get; }

            public Hypothesis Clone()
            {
                return new Hypothesis(this);
            }

            public int CompareTo(Hypothesis other)
            {
                return -Score.CompareTo(other.Score);
            }
        }
    }
}
