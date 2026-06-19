using System.Collections.Generic;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot
{
    public class ThotEflomalWordAlignmentModel : ThotWordAlignmentModel
    {
        public ThotEflomalWordAlignmentModel() { }

        public ThotEflomalWordAlignmentModel(string prefFileName, bool createNew = false)
            : base(prefFileName, createNew) { }

        public override ThotWordAlignmentModelType Type => ThotWordAlignmentModelType.Eflomal;

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
                    wordPair.AlignmentScore = GetTranslationProbability(sourceWord, targetWord);
                }
            }
        }
    }
}
