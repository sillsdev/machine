namespace SIL.Machine.SequenceAlignment
{
	public interface IPairwiseAlignmentScorer<in TSeq, in TItem>
	{
		int GetGapPenalty(TSeq sequence1, TSeq sequence2);
		int GetInsertionScore(TSeq sequence1, TItem p, TSeq sequence2, TItem q);
		int GetDeletionScore(TSeq sequence1, TItem p, TSeq sequence2, TItem q);
		int GetSubstitutionScore(TSeq sequence1, TItem p, TSeq sequence2, TItem q);
		int GetExpansionScore(TSeq sequence1, TItem p, TSeq sequence2, TItem q1, TItem q2);
		int GetCompressionScore(TSeq sequence1, TItem p1, TItem p2, TSeq sequence2, TItem q);
		int GetMaxScore1(TSeq sequence1, TItem p, TSeq sequence2);
		int GetMaxScore2(TSeq sequence1, TSeq sequence2, TItem q);
	}
}
