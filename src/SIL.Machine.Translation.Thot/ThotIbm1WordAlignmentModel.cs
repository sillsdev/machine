using System.Collections.Generic;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot
{
    public class ThotIbm1WordAlignmentModel : ThotWordAlignmentModel
    {
        public ThotIbm1WordAlignmentModel() { }

        public ThotIbm1WordAlignmentModel(string prefFileName, bool createNew = false)
            : base(prefFileName, createNew) { }

        public override ThotWordAlignmentModelType Type => ThotWordAlignmentModelType.Ibm1;

        public double GetAlignmentProbability(int sourceLen)
        {
            CheckDisposed();

            return 1.0 / (sourceLen + 1);
        }

        public override void ComputeAlignedWordPairScores(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            IReadOnlyCollection<AlignedWordPair> wordPairs
        )
        {
            double alignmentScore = GetAlignmentProbability(sourceSegment.Count);
            foreach (AlignedWordPair wordPair in wordPairs)
            {
                if (wordPair.TargetIndex == -1)
                {
                    wordPair.TranslationScore = 0;
                    wordPair.AlignmentScore = 0;
                }
                else
                {
                    string sourceWord = wordPair.SourceIndex == -1 ? null : sourceSegment[wordPair.SourceIndex];
                    string targetWord = targetSegment[wordPair.TargetIndex];
                    wordPair.TranslationScore = GetTranslationProbability(sourceWord, targetWord);
                    wordPair.AlignmentScore = alignmentScore;
                }
            }
        }
    }
}
