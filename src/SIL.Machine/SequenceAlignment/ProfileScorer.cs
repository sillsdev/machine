using System;

namespace SIL.Machine.SequenceAlignment
{
    public class ProfileScorer<TSeq, TItem> : IPairwiseAlignmentScorer<Profile<TSeq, TItem>, AlignmentCell<TItem>[]>
    {
        private readonly IPairwiseAlignmentScorer<TSeq, TItem> _scorer;

        public ProfileScorer(IPairwiseAlignmentScorer<TSeq, TItem> scorer)
        {
            _scorer = scorer;
        }

        public int GetGapPenalty(Profile<TSeq, TItem> sequence1, Profile<TSeq, TItem> sequence2)
        {
            int sum = 0;
            for (int i = 0; i < sequence1.Alignment.SequenceCount; i++)
            {
                for (int j = 0; j < sequence2.Alignment.SequenceCount; j++)
                {
                    sum += _scorer.GetGapPenalty(sequence1.Alignment.Sequences[i], sequence2.Alignment.Sequences[j]);
                }
            }

            return sum / (sequence1.Alignment.SequenceCount * sequence2.Alignment.SequenceCount);
        }

        public int GetInsertionScore(
            Profile<TSeq, TItem> sequence1,
            AlignmentCell<TItem>[] p,
            Profile<TSeq, TItem> sequence2,
            AlignmentCell<TItem>[] q
        )
        {
            int sum = 0;
            for (int i = 0; i < sequence1.Alignment.SequenceCount; i++)
            {
                for (int j = 0; j < sequence2.Alignment.SequenceCount; j++)
                {
                    if ((p == null || !p[i].IsNull) && !q[j].IsNull)
                    {
                        sum += (int)(
                            _scorer.GetInsertionScore(
                                sequence1.Alignment.Sequences[i],
                                p == null ? default : p[i][0],
                                sequence2.Alignment.Sequences[j],
                                q[j][0]
                            )
                            * sequence1.Weights[i]
                            * sequence2.Weights[j]
                        );
                    }
                }
            }

            return sum / (sequence1.Alignment.SequenceCount * sequence2.Alignment.SequenceCount);
        }

        public int GetDeletionScore(
            Profile<TSeq, TItem> sequence1,
            AlignmentCell<TItem>[] p,
            Profile<TSeq, TItem> sequence2,
            AlignmentCell<TItem>[] q
        )
        {
            int sum = 0;
            for (int i = 0; i < sequence1.Alignment.SequenceCount; i++)
            {
                for (int j = 0; j < sequence2.Alignment.SequenceCount; j++)
                {
                    if (!p[i].IsNull && (q == null || !q[j].IsNull))
                    {
                        sum += (int)(
                            _scorer.GetDeletionScore(
                                sequence1.Alignment.Sequences[i],
                                p[i][0],
                                sequence2.Alignment.Sequences[j],
                                q == null ? default : q[j][0]
                            )
                            * sequence1.Weights[i]
                            * sequence2.Weights[j]
                        );
                    }
                }
            }

            return sum / (sequence1.Alignment.SequenceCount * sequence2.Alignment.SequenceCount);
        }

        public int GetSubstitutionScore(
            Profile<TSeq, TItem> sequence1,
            AlignmentCell<TItem>[] p,
            Profile<TSeq, TItem> sequence2,
            AlignmentCell<TItem>[] q
        )
        {
            int sum = 0;
            for (int i = 0; i < sequence1.Alignment.SequenceCount; i++)
            {
                for (int j = 0; j < sequence2.Alignment.SequenceCount; j++)
                {
                    if (!p[i].IsNull && !q[j].IsNull)
                    {
                        sum += (int)(
                            _scorer.GetSubstitutionScore(
                                sequence1.Alignment.Sequences[i],
                                p[i][0],
                                sequence2.Alignment.Sequences[j],
                                q[j][0]
                            )
                            * sequence1.Weights[i]
                            * sequence2.Weights[j]
                        );
                    }
                }
            }

            return sum / (sequence1.Alignment.SequenceCount * sequence2.Alignment.SequenceCount);
        }

        public int GetExpansionScore(
            Profile<TSeq, TItem> sequence1,
            AlignmentCell<TItem>[] p,
            Profile<TSeq, TItem> sequence2,
            AlignmentCell<TItem>[] q1,
            AlignmentCell<TItem>[] q2
        )
        {
            throw new NotImplementedException();
        }

        public int GetCompressionScore(
            Profile<TSeq, TItem> sequence1,
            AlignmentCell<TItem>[] p1,
            AlignmentCell<TItem>[] p2,
            Profile<TSeq, TItem> sequence2,
            AlignmentCell<TItem>[] q
        )
        {
            throw new NotImplementedException();
        }

        public int GetTranspositionScore(
            Profile<TSeq, TItem> sequence1,
            AlignmentCell<TItem>[] p1,
            AlignmentCell<TItem>[] p2,
            Profile<TSeq, TItem> sequence2,
            AlignmentCell<TItem>[] q1,
            AlignmentCell<TItem>[] q2
        )
        {
            throw new NotImplementedException();
        }

        public int GetMaxScore1(
            Profile<TSeq, TItem> sequence1,
            AlignmentCell<TItem>[] p,
            Profile<TSeq, TItem> sequence2
        )
        {
            int sum = 0;
            for (int i = 0; i < sequence1.Alignment.SequenceCount; i++)
            {
                for (int j = 0; j < sequence2.Alignment.SequenceCount; j++)
                {
                    if (!p[i].IsNull)
                    {
                        sum += (int)(
                            _scorer.GetMaxScore1(
                                sequence1.Alignment.Sequences[i],
                                p[i][0],
                                sequence2.Alignment.Sequences[j]
                            )
                            * sequence1.Weights[i]
                            * sequence2.Weights[j]
                        );
                    }
                }
            }

            return sum / (sequence1.Alignment.SequenceCount * sequence2.Alignment.SequenceCount);
        }

        public int GetMaxScore2(
            Profile<TSeq, TItem> sequence1,
            Profile<TSeq, TItem> sequence2,
            AlignmentCell<TItem>[] q
        )
        {
            int sum = 0;
            for (int i = 0; i < sequence1.Alignment.SequenceCount; i++)
            {
                for (int j = 0; j < sequence2.Alignment.SequenceCount; j++)
                {
                    if (!q[j].IsNull)
                    {
                        sum += (int)(
                            _scorer.GetMaxScore2(
                                sequence1.Alignment.Sequences[i],
                                sequence2.Alignment.Sequences[j],
                                q[j][0]
                            )
                            * sequence1.Weights[i]
                            * sequence2.Weights[j]
                        );
                    }
                }
            }

            return sum / (sequence1.Alignment.SequenceCount * sequence2.Alignment.SequenceCount);
        }
    }
}
