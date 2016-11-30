using System.Collections.Generic;
using SIL.Machine.SequenceAlignment;

namespace SIL.Machine.Translation.Thot
{
	public class SegmentScorer : IPairwiseAlignmentScorer<IReadOnlyList<string>, int>
	{
		private const int MaxValue = 100000;

		private readonly ISegmentAligner _segmentAligner;

		public SegmentScorer(ISegmentAligner segmentAligner)
		{
			_segmentAligner = segmentAligner;
		}

		public int GetGapPenalty(IReadOnlyList<string> sequence1, IReadOnlyList<string> sequence2)
		{
			return -(MaxValue / 10);
		}

		public int GetInsertionScore(IReadOnlyList<string> sequence1, int p, IReadOnlyList<string> sequence2, int q)
		{
			string targetWord = sequence2[q];
			return (int) (_segmentAligner.GetTranslationProbability(null, targetWord) * MaxValue);
		}

		public int GetDeletionScore(IReadOnlyList<string> sequence1, int p, IReadOnlyList<string> sequence2, int q)
		{
			string sourceWord = sequence1[p];
			return (int) (_segmentAligner.GetTranslationProbability(sourceWord, null) * MaxValue);
		}

		public int GetSubstitutionScore(IReadOnlyList<string> sequence1, int p, IReadOnlyList<string> sequence2, int q)
		{
			string sourceWord = sequence1[p];
			string targetWord = sequence2[q];
			return (int) ((sourceWord == targetWord ? 1.0 : _segmentAligner.GetTranslationProbability(sourceWord, targetWord)) * MaxValue);
		}

		public int GetExpansionScore(IReadOnlyList<string> sequence1, int p, IReadOnlyList<string> sequence2, int q1, int q2)
		{
			return (GetSubstitutionScore(sequence1, p, sequence2, q1) + GetSubstitutionScore(sequence1, p, sequence2, q2)) / 2;
		}

		public int GetCompressionScore(IReadOnlyList<string> sequence1, int p1, int p2, IReadOnlyList<string> sequence2, int q)
		{
			return (GetSubstitutionScore(sequence1, p1, sequence2, q) + GetSubstitutionScore(sequence1, p2, sequence2, q)) / 2;
		}

		public int GetTranspositionScore(IReadOnlyList<string> sequence1, int p1, int p2, IReadOnlyList<string> sequence2, int q1, int q2)
		{
			return (GetSubstitutionScore(sequence1, p1, sequence2, q2) + GetSubstitutionScore(sequence1, p2, sequence2, q1)) / 2;
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