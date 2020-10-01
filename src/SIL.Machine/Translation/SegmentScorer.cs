using System;
using System.Collections.Generic;
using SIL.Machine.SequenceAlignment;

namespace SIL.Machine.Translation
{
	public class SegmentScorer : IPairwiseAlignmentScorer<IReadOnlyList<string>, int>
	{
		private const int MaxValue = 100000;

		private readonly Func<IReadOnlyList<string>, int, IReadOnlyList<string>, int, double> _scoreSelector;

		public SegmentScorer(Func<IReadOnlyList<string>, int, IReadOnlyList<string>, int, double> scoreSelector)
		{
			_scoreSelector = scoreSelector;
		}

		public int GetGapPenalty(IReadOnlyList<string> sequence1, IReadOnlyList<string> sequence2)
		{
			return -(MaxValue / 10);
		}

		public int GetInsertionScore(IReadOnlyList<string> sequence1, int p, IReadOnlyList<string> sequence2, int q)
		{
			return (int) (_scoreSelector(sequence1, -1, sequence2, q) * MaxValue);
		}

		public int GetDeletionScore(IReadOnlyList<string> sequence1, int p, IReadOnlyList<string> sequence2, int q)
		{
			return (int) (_scoreSelector(sequence1, p, sequence2, -1) * MaxValue);
		}

		public int GetSubstitutionScore(IReadOnlyList<string> sequence1, int p, IReadOnlyList<string> sequence2, int q)
		{
			string sourceWord = sequence1[p];
			string targetWord = sequence2[q];
			return (int) ((sourceWord == targetWord ? 1.0 : _scoreSelector(sequence1, p, sequence2, q)) * MaxValue);
		}

		public int GetExpansionScore(IReadOnlyList<string> sequence1, int p, IReadOnlyList<string> sequence2, int q1,
			int q2)
		{
			return (GetSubstitutionScore(sequence1, p, sequence2, q1)
				+ GetSubstitutionScore(sequence1, p, sequence2, q2)) / 2;
		}

		public int GetCompressionScore(IReadOnlyList<string> sequence1, int p1, int p2, IReadOnlyList<string> sequence2,
			int q)
		{
			return (GetSubstitutionScore(sequence1, p1, sequence2, q)
				+ GetSubstitutionScore(sequence1, p2, sequence2, q)) / 2;
		}

		public int GetTranspositionScore(IReadOnlyList<string> sequence1, int p1, int p2,
			IReadOnlyList<string> sequence2, int q1, int q2)
		{
			return (GetSubstitutionScore(sequence1, p1, sequence2, q2)
				+ GetSubstitutionScore(sequence1, p2, sequence2, q1)) / 2;
		}

		public int GetMaxScore1(IReadOnlyList<string> sequence1, int p, IReadOnlyList<string> sequence2)
		{
			return MaxValue;
		}

		public int GetMaxScore2(IReadOnlyList<string> sequence1, IReadOnlyList<string> sequence2, int q)
		{
			return MaxValue;
		}
	}
}