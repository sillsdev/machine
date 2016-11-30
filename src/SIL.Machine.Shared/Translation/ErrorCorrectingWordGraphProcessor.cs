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
		private readonly List<List<int>> _stateBestPrevArcs;
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
			_stateBestPrevArcs = new List<List<int>>();
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
				_stateBestPrevArcs.Add(new List<int>());
			}

			_ecm.SetupInitialEsi(_stateEcmScoreInfos[WordGraph.InitialState]);
			UpdateInitialStateBestScores();
		}

		private void InitArcs()
		{
			for (int arcIndex = 0; arcIndex < _wordGraph.Arcs.Count; arcIndex++)
			{
				WordGraphArc arc = _wordGraph.Arcs[arcIndex];

				// init ecm score info for each word of arc
				EcmScoreInfo prevEsi = _stateEcmScoreInfos[arc.PrevState];
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

		public IEnumerable<TranslationInfo> Correct(IReadOnlyList<string> prefix, bool isLastWordComplete, int n)
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

			var candidates = new List<Candidate>();
			GetNBestStateCandidates(candidates, n);
			GetNBestSubStateCandidates(candidates, n);

			TranslationInfo[] nbestCorrections = candidates.Select(c => GetCorrectionForCandidate(prefix, isLastWordComplete, c)).ToArray();

			_prevPrefix = prefix.ToArray();
			_prevIsLastWordComplete = isLastWordComplete;

			return nbestCorrections;
		}

		private void ProcessWordGraphForPrefixDiff(IReadOnlyList<string> prefixDiff, bool isLastWordComplete)
		{
			if (prefixDiff.Count == 0)
				return;

			EcmScoreInfo prevInitialEsi = _stateEcmScoreInfos[WordGraph.InitialState];
			_ecm.ExtendInitialEsi(_stateEcmScoreInfos[WordGraph.InitialState], prevInitialEsi, prefixDiff);
			UpdateInitialStateBestScores();

			for (int arcIndex = 0; arcIndex < _wordGraph.Arcs.Count; arcIndex++)
			{
				WordGraphArc arc = _wordGraph.Arcs[arcIndex];

				// update ecm score info for each word of arc
				EcmScoreInfo prevEsi = _stateEcmScoreInfos[arc.PrevState];
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

		private void GetNBestStateCandidates(List<Candidate> candidates, int n)
		{
			foreach (int state in _statesInvolvedInArcs)
			{
				double restScore = _restScores[state];
				List<double> bestScores = _stateBestScores[state];

				double score = bestScores[bestScores.Count - 1] + (WordGraphWeight * restScore);
				AddToNBestList(candidates, n, new Candidate(score, state));
			}
		}

		private void GetNBestSubStateCandidates(List<Candidate> candidates, int n)
		{
			for (int arcIndex = 0; arcIndex < _wordGraph.Arcs.Count; arcIndex++)
			{
				WordGraphArc arc = _wordGraph.Arcs[arcIndex];
				if (arc.Words.Count > 1)
				{
					double wordGraphScore = _stateWordGraphScores[arc.PrevState];

					for (int i = 0; i < arc.Words.Count - 1; i++)
					{
						EcmScoreInfo esi = _arcEcmScoreInfos[arcIndex][i];
						double score = (WordGraphWeight * wordGraphScore) + (EcmWeight * -esi.Scores[esi.Scores.Count - 1]) + (WordGraphWeight * _restScores[arc.PrevState]);
						AddToNBestList(candidates, n, new Candidate(score, arc.NextState, arcIndex, i));
					}
				}
			}
		}

		private TranslationInfo GetCorrectionForCandidate(IReadOnlyList<string> prefix, bool isLastWordComplete, Candidate candidate)
		{
			var correction = new TranslationInfo {Score = candidate.Score};

			int uncorrectedPrefixLen;
			if (candidate.ArcIndex == -1)
			{
				AddBestUncorrectedPrefixState(correction, prefix.Count, candidate.State);
				uncorrectedPrefixLen = correction.Target.Count;
			}
			else
			{
				AddBestUncorrectedPrefixSubState(correction, prefix.Count, candidate.ArcIndex, candidate.ArcWordIndex);
				WordGraphArc firstArc = _wordGraph.Arcs[candidate.ArcIndex];
				uncorrectedPrefixLen = correction.Target.Count - firstArc.Words.Count - candidate.ArcWordIndex + 1;
			}

			int alignmentColsToAddCount = _ecm.CorrectPrefix(correction, uncorrectedPrefixLen, prefix, isLastWordComplete);

			foreach (WordGraphArc arc in _wordGraph.GetBestPathFromFinalStateToState(candidate.State).Reverse())
				UpdateCorrectionFromArc(correction, arc, false, alignmentColsToAddCount);

			return correction;
		}

		private void AddBestUncorrectedPrefixState(TranslationInfo correction, int procPrefixPos, int state)
		{
			var arcs = new Stack<WordGraphArc>();

			int curState = state;
			int curProcPrefixPos = procPrefixPos;
			while (curState != 0)
			{
				int arcIndex = _stateBestPrevArcs[curState][curProcPrefixPos];
				WordGraphArc arc = _wordGraph.Arcs[arcIndex];

				for (int i = arc.Words.Count - 1; i >= 0; i--)
				{
					IReadOnlyList<int> predPrefixWords = _arcEcmScoreInfos[arcIndex][i].GetLastInsPrefixWordFromEsi();
					curProcPrefixPos = predPrefixWords[curProcPrefixPos];
				}

				arcs.Push(arc);

				curState = arc.PrevState;
			}

			foreach (WordGraphArc arc in arcs)
				UpdateCorrectionFromArc(correction, arc, true, 0);
		}

		private void AddBestUncorrectedPrefixSubState(TranslationInfo correction, int procPrefixPos, int arcIndex, int arcWordIndex)
		{
			WordGraphArc arc = _wordGraph.Arcs[arcIndex];

			int curProcPrefixPos = procPrefixPos;
			for (int i = arcWordIndex; i >= 0; i--)
			{
				IReadOnlyList<int> predPrefixWords = _arcEcmScoreInfos[arcIndex][i].GetLastInsPrefixWordFromEsi();
				curProcPrefixPos = predPrefixWords[curProcPrefixPos];
			}

			AddBestUncorrectedPrefixState(correction, curProcPrefixPos, arc.PrevState);

			UpdateCorrectionFromArc(correction, arc, true, 0);
		}

		private void UpdateCorrectionFromArc(TranslationInfo correction, WordGraphArc arc, bool isPrefix, int alignmentColsToAddCount)
		{
			for (int i = 0; i < arc.Words.Count; i++)
			{
				correction.Target.Add(arc.Words[i]);
				correction.TargetConfidences.Add(arc.WordConfidences[i]);
				if (!isPrefix && arc.IsUnknown)
					correction.TargetUnknownWords.Add(correction.Target.Count - 1);
			}

			WordAlignmentMatrix alignment = arc.Alignment;
			if (alignmentColsToAddCount > 0)
			{
				var newAlignment = new WordAlignmentMatrix(alignment.I, alignment.J + alignmentColsToAddCount);
				for (int j = 0; j < alignment.J; j++)
				{
					for (int i = 0; i < alignment.I; i++)
						newAlignment[i, alignmentColsToAddCount + j] = alignment[i, j];
				}
				alignment = newAlignment;
			}

			var phrase = new PhraseInfo
			{
				SourceStartIndex = arc.SourceStartIndex,
				SourceEndIndex = arc.SourceEndIndex,
				TargetCut = correction.Target.Count - 1,
				Alignment = alignment
			};
			correction.Phrases.Add(phrase);
		}

		private static void AddToNBestList<T>(List<T> nbestList, int n, T item) where T : IComparable<T>
		{
			int index = nbestList.BinarySearch(item);
			if (index < 0)
				index = ~index;
			else
				index++;
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

		private class Candidate : IComparable<Candidate>
		{
			public Candidate(double score, int state, int arcIndex = -1, int arcWordIndex = -1)
			{
				Score = score;
				State = state;
				ArcIndex = arcIndex;
				ArcWordIndex = arcWordIndex;
			}

			public double Score { get; }
			public int State { get; }
			public int ArcIndex { get; }
			public int ArcWordIndex { get; }

			public int CompareTo(Candidate other)
			{
				return -Score.CompareTo(other.Score);
			}
		}
	}
}
