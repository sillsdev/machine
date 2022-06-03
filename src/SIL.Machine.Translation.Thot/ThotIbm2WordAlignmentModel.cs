using System.Collections.Generic;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot
{
    public class ThotIbm2WordAlignmentModel : ThotIbm1WordAlignmentModel, IIbm2WordAlignmentModel
    {
        public ThotIbm2WordAlignmentModel() { }

        public ThotIbm2WordAlignmentModel(string prefFileName, bool createNew = false) : base(prefFileName, createNew)
        { }

        public override ThotWordAlignmentModelType Type => ThotWordAlignmentModelType.Ibm2;

        public double GetAlignmentProbability(int sourceLen, int sourceIndex, int targetLen, int targetIndex)
        {
            CheckDisposed();
            if (sourceIndex == -1)
                return 0;

            // add 1 to convert the specified indices to Thot position indices, which are 1-based
            return Thot.swAlignModel_getIbm2AlignmentProbability(
                Handle,
                (uint)(targetIndex + 1),
                (uint)sourceLen,
                (uint)targetLen,
                (uint)(sourceIndex + 1)
            );
        }

        public override void ComputeAlignedWordPairScores(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            IReadOnlyCollection<AlignedWordPair> wordPairs
        )
        {
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
                    wordPair.AlignmentScore = GetAlignmentProbability(
                        sourceSegment.Count,
                        wordPair.SourceIndex,
                        targetSegment.Count,
                        wordPair.TargetIndex
                    );
                }
            }
        }
    }
}
