using System;
using System.Collections.Generic;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
    public interface IWordAlignmentEngine : IWordAligner, IDisposable
    {
        IWordVocabulary SourceWords { get; }
        IWordVocabulary TargetWords { get; }
        IReadOnlySet<int> SpecialSymbolIndices { get; }

        IEnumerable<(string TargetWord, double Score)> GetTranslations(string sourceWord, double threshold = 0);
        IEnumerable<(int TargetWordIndex, double Score)> GetTranslations(int sourceWordIndex, double threshold = 0);

        double GetTranslationScore(string sourceWord, string targetWord);
        double GetTranslationScore(int sourceWordIndex, int targetWordIndex);

        IReadOnlyCollection<AlignedWordPair> GetBestAlignedWordPairs(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment
        );
        void ComputeAlignedWordPairScores(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            IReadOnlyCollection<AlignedWordPair> wordPairs
        );
    }
}
