using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class ErrorCorrectingWordGraphProcessor
	{
		private readonly WordGraph _wordGraph;
		private readonly double[] _restScores;
		private readonly ErrorCorrectingModel _ecm;
		private readonly List<EcmScoreInfo> _stateEcmScoreInfos;
		private readonly List<List<EcmScoreInfo>> _arcEcmScoreInfos;
		private readonly List<List<double>> _stateBestScores;
		private readonly List<double> _stateWordGraphScores;
		private readonly List<List<int>> _stateBestPreds;
		private readonly HashSet<int> _statesInvolvedInArcs;
		private string[] _prevPrefix;
		private bool _prevIsLastWordComplete;

		public ErrorCorrectingWordGraphProcessor(ErrorCorrectingModel ecm, WordGraph wordGraph, double ecmWeight = 1, double wordGraphWeight = 1)
		{
			_ecm = ecm;
			_wordGraph = wordGraph;
			EcmWeight = ecmWeight;
			WordGraphWeight = wordGraphWeight;

			_restScores = _wordGraph.ComputeRestScores().ToArray();
			_stateEcmScoreInfos = new List<EcmScoreInfo>();
			_arcEcmScoreInfos = new List<List<EcmScoreInfo>>();
			_stateBestScores = new List<List<double>>();
			_stateWordGraphScores = new List<double>();
			_stateBestPreds = new List<List<int>>();
			_statesInvolvedInArcs = new HashSet<int>();
			_prevPrefix = new string[0];

			InitStates();
			InitArcs();
		}

		private void InitStates()
		{
			for (int i = 0; i < _wordGraph.StateCount; i++)
			{
				_stateEcmScoreInfos.Add(new EcmScoreInfo());
				_stateWordGraphScores.Add(0);
				_stateBestScores.Add(new List<double>());
				_stateBestPreds.Add(new List<int>());
			}

			_ecm.SetupInitialEsi(_stateEcmScoreInfos[0]);
			UpdateInitialStateBestScores();
		}

		private void InitArcs()
		{
			for (int arcIndex = 0; arcIndex < _wordGraph.Arcs.Count; arcIndex++)
			{
				WordGraphArc arc = _wordGraph.Arcs[arcIndex];

				// init ecm score info for each word of arc
				EcmScoreInfo prevEsi = _stateEcmScoreInfos[arc.PredStateIndex];
				var esis = new List<EcmScoreInfo>();
				foreach (string word in arc.Words)
				{
					var esi = new EcmScoreInfo();
					_ecm.SetupEsi(esi, prevEsi, word);
					esis.Add(esi);
					prevEsi = esi;
				}
				_arcEcmScoreInfos.Add(esis);

				// init best scores for the arc's successive state
				UpdateStateBestScores(arcIndex, 0);

				_statesInvolvedInArcs.Add(arc.PredStateIndex);
				_statesInvolvedInArcs.Add(arc.SuccStateIndex);
			}
		}

		private void UpdateInitialStateBestScores()
		{
			EcmScoreInfo esi = _stateEcmScoreInfos[0];

			_stateWordGraphScores[0] = _wordGraph.InitialStateScore;

			List<double> bestScores = _stateBestScores[0];
			List<int> bestPreds = _stateBestPreds[0];

			bestScores.Clear();
			bestPreds.Clear();

			foreach (double score in esi.Scores)
			{
				bestScores.Add((EcmWeight * -score) + (WordGraphWeight * _wordGraph.InitialStateScore));
				bestPreds.Add(int.MaxValue);
			}
		}

		private void UpdateStateBestScores(int arcIndex, int prefixDiffSize)
		{
			WordGraphArc arc = _wordGraph.Arcs[arcIndex];
			List<EcmScoreInfo> arcEsis = _arcEcmScoreInfos[arcIndex];

			EcmScoreInfo prevEsi = arcEsis.Count == 0 ? _stateEcmScoreInfos[arc.PredStateIndex] : arcEsis[arcEsis.Count - 1];

			double wordGraphScore = _stateWordGraphScores[arc.PredStateIndex] + arc.Score;

			List<double> succStateBestScores = _stateBestScores[arc.SuccStateIndex];
			List<int> succStateBestPreds = _stateBestPreds[arc.SuccStateIndex];

			var positions = new List<int>();
			int startPos = prefixDiffSize == 0 ? 0 : prevEsi.Scores.Count - prefixDiffSize;
			for (int i = startPos; i < prevEsi.Scores.Count; i++)
			{
				double newScore = (EcmWeight * -prevEsi.Scores[i]) + (WordGraphWeight * wordGraphScore);

				if (i == succStateBestScores.Count || succStateBestScores[i] < newScore)
				{
					AddOrReplace(succStateBestScores, i, newScore);
					positions.Add(i);
					AddOrReplace(succStateBestPreds, i, arcIndex);
				}
			}

			_stateEcmScoreInfos[arc.SuccStateIndex].UpdatePositions(prevEsi, positions);

			_stateWordGraphScores[arc.SuccStateIndex] = wordGraphScore;
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

		public IEnumerable<TranslationData> Correct(IReadOnlyList<string> prefix, bool isLastWordComplete, int n)
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
							esi.RemoveLastPosition();
					}
				}

				// adjust size of info for states
				foreach (int stateIndex in _statesInvolvedInArcs)
				{
					for (int i = 0; i < diffSize; i++)
					{
						_stateEcmScoreInfos[stateIndex].RemoveLastPosition();
						_stateBestScores[stateIndex].RemoveAt(_stateBestScores[stateIndex].Count - 1);
						_stateBestPreds[stateIndex].RemoveAt(_stateBestPreds[stateIndex].Count - 1);
					}
				}
			}

			// get difference between prefix and valid portion of processed prefix
			var prefixDiff = new string[prefix.Count - validProcPrefixCount];
			for (int i = 0; i < prefixDiff.Length; i++)
				prefixDiff[i] = prefix[validProcPrefixCount + i];

			// process word-graph given prefix difference
			ProcessWordGraphForPrefixDiff(prefixDiff, isLastWordComplete);

			IEnumerable<HypState> nbestHypStates = GetNBestHypStates(n);
			IEnumerable<HypSubState> nbestHypSubStates = GetNBestHypSubStates(n);

			IEnumerable<TranslationData> nbestCorrections = GetNBestCorrections(prefix, isLastWordComplete, n, nbestHypStates, nbestHypSubStates);

			_prevPrefix = prefix.ToArray();
			_prevIsLastWordComplete = isLastWordComplete;
			return nbestCorrections;
		}

		private void ProcessWordGraphForPrefixDiff(IReadOnlyList<string> prefixDiff, bool isLastWordComplete)
		{
			if (prefixDiff.Count == 0)
				return;

			EcmScoreInfo prevInitialEsi = _stateEcmScoreInfos[0];
			_ecm.ExtendInitialEsi(_stateEcmScoreInfos[0], prevInitialEsi, prefixDiff);
			UpdateInitialStateBestScores();

			for (int arcIndex = 0; arcIndex < _wordGraph.Arcs.Count; arcIndex++)
			{
				WordGraphArc arc = _wordGraph.Arcs[arcIndex];

				// update ecm score info for each word of arc
				EcmScoreInfo prevEsi = _stateEcmScoreInfos[arc.PredStateIndex];
				List<EcmScoreInfo> esis = _arcEcmScoreInfos[arcIndex];
				while (esis.Count < arc.Words.Count)
					esis.Add(new EcmScoreInfo());
				for (int i = 0; i < arc.Words.Count; i++)
				{
					EcmScoreInfo esi = esis[i];
					_ecm.ExtendEsi(esi, prevEsi, arc.IsUnknown ? string.Empty : arc.Words[i], prefixDiff, isLastWordComplete);
					prevEsi = esi;
				}

				// update best scores for the arc's successive state
				UpdateStateBestScores(arcIndex, prefixDiff.Count);
			}
		}

		private IEnumerable<HypState> GetNBestHypStates(int n)
		{
			var states = new List<HypState>();
			foreach (int stateIndex in _statesInvolvedInArcs)
			{
				double restScore = _restScores[stateIndex];
				List<double> bestScores = _stateBestScores[stateIndex];

				double score = bestScores[bestScores.Count - 1] + (WordGraphWeight * restScore);
				AddToNBestList(states, n, new HypState(score, stateIndex));
			}
			return states;
		}

		private IEnumerable<HypSubState> GetNBestHypSubStates(int n)
		{
			var subStates = new List<HypSubState>();

			for (int arcIndex = 0; arcIndex < _wordGraph.Arcs.Count; arcIndex++)
			{
				WordGraphArc arc = _wordGraph.Arcs[arcIndex];
				if (arc.Words.Count > 1)
				{
					double wordGraphScore = _stateWordGraphScores[arc.PredStateIndex];

					for (int i = 0; i < arc.Words.Count - 1; i++)
					{
						EcmScoreInfo esi = _arcEcmScoreInfos[arcIndex][i];
						double score = (WordGraphWeight * wordGraphScore) + (EcmWeight * -esi.Scores[esi.Scores.Count - 1]) + (WordGraphWeight * _restScores[arc.PredStateIndex]);
						AddToNBestList(subStates, n, new HypSubState(score, arcIndex, i));
					}
				}
			}

			return subStates;
		}

		private IEnumerable<TranslationData> GetNBestCorrections(IReadOnlyList<string> prefix, bool isLastWordComplete, int n, IEnumerable<HypState> nbestHypStates,
			IEnumerable<HypSubState> nbestHypSubStates)
		{
			var corrections = new List<TranslationData>();
			foreach (HypState state in nbestHypStates)
			{
				TranslationData translationData = GetCorrectionForHypState(prefix, isLastWordComplete, state.StateIndex);
				translationData.Score = state.Score;
				AddToNBestList(corrections, n, translationData);
			}

			foreach (HypSubState subState in nbestHypSubStates)
			{
				TranslationData translationData = GetCorrectionForHypSubState(prefix, isLastWordComplete, subState.ArcIndex, subState.ArcWordIndex);
				translationData.Score = subState.Score;
				AddToNBestList(corrections, n, translationData);
			}

			return corrections;
		}

		private TranslationData GetCorrectionForHypState(IReadOnlyList<string> prefix, bool isLastWordComplete, int stateIndex)
		{
			var correction = new TranslationData();

			IReadOnlyList<string> uncorrectedPrefix = GetBestUncorrectedPrefixHypState(prefix.Count, stateIndex,
				correction.SourceSegmentation, correction.TargetSegmentCuts);

			UpdateCorrectionFromPrefix(correction, uncorrectedPrefix, prefix, isLastWordComplete);

			foreach (WordGraphArc arc in _wordGraph.GetBestPathFromFinalStateToState(stateIndex).Reverse())
				UpdateCorrectionFromArc(correction, arc, 0);

			RemoveLastSpace(correction.Target);

			return correction;
		}

		private IReadOnlyList<string> GetBestUncorrectedPrefixHypState(int procPrefixPos, int stateIndex, IList<Tuple<int, int>> sourceSegmentation,
			IList<int> targetSegmentCuts)
		{
			var results = new Stack<string>();
			var srcSeg = new Stack<Tuple<int, int>>();
			var phraseSizes = new Stack<int>();

			int curStateIndex = stateIndex;
			int curProcPrefixPos = procPrefixPos;
			while (curStateIndex != 0)
			{
				int arcIndex = _stateBestPreds[curStateIndex][curProcPrefixPos];
				WordGraphArc arc = _wordGraph.Arcs[arcIndex];

				for (int i = arc.Words.Count - 1; i >= 0; i--)
				{
					IReadOnlyList<int> predPrefixWords = _ecm.GetLastInsPrefixWordFromEsi(_arcEcmScoreInfos[arcIndex][i]);
					curProcPrefixPos = predPrefixWords[curProcPrefixPos];
				}

				curStateIndex = arc.PredStateIndex;

				foreach (string word in arc.Words.Reverse())
					results.Push(word);

				srcSeg.Push(Tuple.Create(arc.SrcStartIndex, arc.SrcEndIndex));
				phraseSizes.Push(arc.Words.Count);
			}

			foreach (Tuple<int, int> seg in srcSeg)
				sourceSegmentation.Add(seg);

			bool first = true;
			foreach (int phraseSize in phraseSizes)
			{
				int lastPos = first ? 0 : targetSegmentCuts[targetSegmentCuts.Count - 1];
				targetSegmentCuts.Add(lastPos + phraseSize);
				first = false;
			}

			return results.ToArray();
		}

		private TranslationData GetCorrectionForHypSubState(IReadOnlyList<string> prefix, bool isLastWordComplete, int arcIndex, int arcWordIndex)
		{
			var correction = new TranslationData();

			IReadOnlyList<string> uncorrectedPrefix = GetBestUncorrectedPrefixHypSubState(prefix.Count, arcIndex, arcWordIndex,
				correction.SourceSegmentation, correction.TargetSegmentCuts);

			UpdateCorrectionFromPrefix(correction, uncorrectedPrefix, prefix, isLastWordComplete);

			WordGraphArc firstArc = _wordGraph.Arcs[arcIndex];
			UpdateCorrectionFromArc(correction, firstArc, arcWordIndex + 1);

			foreach (WordGraphArc arc in _wordGraph.GetBestPathFromFinalStateToState(firstArc.SuccStateIndex).Reverse())
				UpdateCorrectionFromArc(correction, arc, 0);

			RemoveLastSpace(correction.Target);

			return correction;
		}

		private IReadOnlyList<string> GetBestUncorrectedPrefixHypSubState(int procPrefixPos, int arcIndex, int arcWordIndex,
			IList<Tuple<int, int>> sourceSegmentation, IList<int> targetSegmentCuts)
		{
			WordGraphArc arc = _wordGraph.Arcs[arcIndex];

			int curProcPrefixPos = procPrefixPos;
			for (int i = arcWordIndex; i >= 0; i--)
			{
				IReadOnlyList<int> predPrefixWords = _ecm.GetLastInsPrefixWordFromEsi(_arcEcmScoreInfos[arcIndex][i]);
				curProcPrefixPos = predPrefixWords[curProcPrefixPos];
			}

			IReadOnlyList<string> uncorrectedPrefix = GetBestUncorrectedPrefixHypState(curProcPrefixPos, arc.PredStateIndex, sourceSegmentation, targetSegmentCuts);
			var result = new string[uncorrectedPrefix.Count + arcWordIndex + 1];
			int resultIndex = 0;
			foreach (string word in uncorrectedPrefix)
			{
				result[resultIndex] = word;
				resultIndex++;
			}

			for (int i = 0; i <= arcWordIndex; i++)
			{
				result[resultIndex] = arc.Words[i];
				resultIndex++;
			}

			return result;
		}

		private void UpdateCorrectionFromPrefix(TranslationData translationData, IReadOnlyList<string> uncorrectedPrefix, IReadOnlyList<string> prefix, bool isLastWordComplete)
		{
			if (uncorrectedPrefix.Count == 0)
			{
				foreach (string w in prefix)
					translationData.Target.Add(w);
				RemoveLastSpace(translationData.Target);
			}
			else
			{
				_ecm.CorrectPrefix(uncorrectedPrefix, prefix, isLastWordComplete, translationData.Target, translationData.SourceSegmentation, translationData.TargetSegmentCuts);
			}
		}

		private void UpdateCorrectionFromArc(TranslationData translationData, WordGraphArc arc, int startWordIndex)
		{
			for (int i = startWordIndex; i < arc.Words.Count; i++)
			{
				translationData.Target.Add(arc.Words[i]);
				if (arc.IsUnknown)
					translationData.TargetUnknownWords.Add(translationData.Target.Count);
			}
			translationData.SourceSegmentation.Add(Tuple.Create(arc.SrcStartIndex, arc.SrcEndIndex));
			translationData.TargetSegmentCuts.Add(translationData.Target.Count);
		}

		private static void RemoveLastSpace(IList<string> segment)
		{
			if (segment.Count > 0)
				segment[segment.Count - 1] = segment[segment.Count - 1].TrimEnd();
		}

		private static void AddToNBestList<T>(List<T> nbestList, int n, T item) where T : IComparable<T>
		{
			int index = nbestList.BinarySearch(item);
			if (index < 0)
				index = ~index;
			if (nbestList.Count < n)
			{
				nbestList.Insert(index, item);
			}
			else if (index < nbestList.Count)
			{
				nbestList.Insert(index, item);
				nbestList.RemoveAt(nbestList.Count - 1);
			}
		}

		private class HypState : IComparable<HypState>
		{
			public HypState(double score, int stateIndex)
			{
				Score = score;
				StateIndex = stateIndex;
			}

			public double Score { get; }
			public int StateIndex { get; }

			public int CompareTo(HypState other)
			{
				return -Score.CompareTo(other.Score);
			}
		}

		private class HypSubState : IComparable<HypSubState>
		{
			public HypSubState(double score, int arcIndex, int arcWordIndex)
			{
				Score = score;
				ArcIndex = arcIndex;
				ArcWordIndex = arcWordIndex;
			}

			public double Score { get; }
			public int ArcIndex { get; }
			public int ArcWordIndex { get; }

			public int CompareTo(HypSubState other)
			{
				return -Score.CompareTo(other.Score);
			}
		}
	}
}
