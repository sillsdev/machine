using SIL.Machine.SequenceAlignment;

namespace SIL.Machine.Tests.SequenceAlignment
{
	public class StringScorer : IPairwiseAlignmentScorer<string, char>
	{
		public int GetGapPenalty(string sequence1, string sequence2)
		{
			return -100;
		}

		public int GetInsertionScore(string sequence1, char p, string sequence2, char q)
		{
			return 0;
		}

		public int GetDeletionScore(string sequence1, char p, string sequence2, char q)
		{
			return 0;
		}

		public int GetSubstitutionScore(string sequence1, char p, string sequence2, char q)
		{
			return p == q ? 100 : 0;
		}

		public int GetExpansionScore(string sequence1, char p, string sequence2, char q1, char q2)
		{
			int score = 0;
			if (p == q1)
				score += 100;
			if (p == q2)
				score += 100;
			return score;
		}

		public int GetCompressionScore(string sequence1, char p1, char p2, string sequence2, char q)
		{
			int score = 0;
			if (q == p1)
				score += 100;
			if (q == p2)
				score += 100;
			return score;
		}

		public int GetTranspositionScore(string sequence1, char p1, char p2, string sequence2, char q1, char q2)
		{
			return p1 == q2 && p2 == q1 ? 100 : 0;
		}

		public virtual int GetMaxScore1(string sequence1, char p, string sequence2)
		{
			return 100;
		}

		public virtual int GetMaxScore2(string sequence1, string sequence2, char q)
		{
			return 100;
		}
	}
}
