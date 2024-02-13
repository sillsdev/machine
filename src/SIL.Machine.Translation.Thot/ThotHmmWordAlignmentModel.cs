using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot
{
    public class ThotHmmWordAlignmentModel : ThotIbm1WordAlignmentModel, IHmmWordAlignmentModel
    {
        public ThotHmmWordAlignmentModel() { }

        public ThotHmmWordAlignmentModel(string prefFileName, bool createNew = false)
            : base(prefFileName, createNew) { }

        public override ThotWordAlignmentModelType Type => ThotWordAlignmentModelType.Hmm;

        /// <summary>
        /// Gets the alignment probability from the HMM single word alignment model. Use -1 for unaligned indices that
        /// occur before the first aligned index. Other unaligned indices are indicated by adding the source length to
        /// the previously aligned index.
        /// </summary>
        public double GetAlignmentProbability(int sourceLen, int prevSourceIndex, int sourceIndex)
        {
            CheckDisposed();

            // add 1 to convert the specified indices to Thot position indices, which are 1-based
            return Thot.swAlignModel_getHmmAlignmentProbability(
                Handle,
                (uint)(prevSourceIndex + 1),
                (uint)sourceLen,
                (uint)(sourceIndex + 1)
            );
        }

        public override void ComputeAlignedWordPairScores(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            IReadOnlyCollection<AlignedWordPair> wordPairs
        )
        {
            int[] sourceIndices = Enumerable.Repeat(-2, targetSegment.Count).ToArray();
            int prevSourceIndex = -1;
            var alignedTargetIndices = new HashSet<int>();
            foreach (AlignedWordPair wordPair in wordPairs.OrderBy(wp => wp.TargetIndex))
            {
                if (wordPair.TargetIndex == -1)
                    continue;
                if (sourceIndices[wordPair.TargetIndex] == -2)
                {
                    int sourceIndex = wordPair.SourceIndex;
                    if (sourceIndex == -1)
                        sourceIndex = prevSourceIndex == -1 ? -1 : sourceSegment.Count + prevSourceIndex;
                    sourceIndices[wordPair.TargetIndex] = sourceIndex;
                    prevSourceIndex = sourceIndex;
                }
                alignedTargetIndices.Add(wordPair.TargetIndex);
            }

            if (alignedTargetIndices.Count < targetSegment.Count)
            {
                // there are target words that are aligned to NULL, so fill in the correct source index
                prevSourceIndex = -1;
                for (int j = 0; j < targetSegment.Count; j++)
                {
                    if (sourceIndices[j] == -2)
                    {
                        int sourceIndex = prevSourceIndex == -1 ? -1 : sourceSegment.Count + prevSourceIndex;
                        sourceIndices[j] = sourceIndex;
                        prevSourceIndex = sourceIndex;
                    }
                    else
                        prevSourceIndex = sourceIndices[j];
                }
            }

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
                    prevSourceIndex = wordPair.TargetIndex == 0 ? -1 : sourceIndices[wordPair.TargetIndex - 1];
                    int sourceIndex = sourceIndices[wordPair.TargetIndex];
                    wordPair.AlignmentScore = GetAlignmentProbability(
                        sourceSegment.Count,
                        prevSourceIndex,
                        sourceIndex
                    );
                }
            }
        }
    }
}
