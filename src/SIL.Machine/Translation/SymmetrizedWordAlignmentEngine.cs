using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
    public class SymmetrizedWordAlignmentEngine : DisposableBase, IWordAlignmentEngine
    {
        private readonly IWordAlignmentEngine _directWordAlignmentEngine;
        private readonly IWordAlignmentEngine _inverseWordAlignmentEngine;
        private readonly SymmetrizedWordAligner _aligner;

        public SymmetrizedWordAlignmentEngine(
            IWordAlignmentEngine directWordAlignmentEngine,
            IWordAlignmentEngine inverseWordAlignmentEngine
        )
        {
            _directWordAlignmentEngine = directWordAlignmentEngine;
            _inverseWordAlignmentEngine = inverseWordAlignmentEngine;
            _aligner = new SymmetrizedWordAligner(DirectWordAlignmentEngine, InverseWordAlignmentEngine);
        }

        public SymmetrizationHeuristic Heuristic
        {
            get => _aligner.Heuristic;
            set => _aligner.Heuristic = value;
        }

        public IWordAlignmentEngine DirectWordAlignmentEngine
        {
            get
            {
                CheckDisposed();

                return _directWordAlignmentEngine;
            }
        }

        public IWordAlignmentEngine InverseWordAlignmentEngine
        {
            get
            {
                CheckDisposed();

                return _inverseWordAlignmentEngine;
            }
        }

        public IWordVocabulary SourceWords
        {
            get
            {
                CheckDisposed();

                return _directWordAlignmentEngine.SourceWords;
            }
        }

        public IWordVocabulary TargetWords
        {
            get
            {
                CheckDisposed();

                return _directWordAlignmentEngine.TargetWords;
            }
        }

        public IReadOnlySet<int> SpecialSymbolIndices => _directWordAlignmentEngine.SpecialSymbolIndices;

        public WordAlignmentMatrix Align(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
        {
            CheckDisposed();

            return _aligner.Align(sourceSegment, targetSegment);
        }

        public IReadOnlyList<WordAlignmentMatrix> AlignBatch(
            IReadOnlyList<(IReadOnlyList<string> SourceSegment, IReadOnlyList<string> TargetSegment)> segments
        )
        {
            CheckDisposed();

            return _aligner.AlignBatch(segments);
        }

        public IEnumerable<(string TargetWord, double Score)> GetTranslations(string sourceWord, double threshold = 0)
        {
            CheckDisposed();

            foreach ((string targetWord, double dirScore) in _directWordAlignmentEngine.GetTranslations(sourceWord))
            {
                double invScore = _inverseWordAlignmentEngine.GetTranslationScore(targetWord, sourceWord);
                double score = Math.Max(dirScore, invScore);
                if (score > threshold)
                    yield return (targetWord, score);
            }
        }

        public IEnumerable<(int TargetWordIndex, double Score)> GetTranslations(
            int sourceWordIndex,
            double threshold = 0
        )
        {
            CheckDisposed();

            foreach (
                (int targetWordIndex, double dirScore) in _directWordAlignmentEngine.GetTranslations(sourceWordIndex)
            )
            {
                double invScore = _inverseWordAlignmentEngine.GetTranslationScore(targetWordIndex, sourceWordIndex);
                double score = Math.Max(dirScore, invScore);
                if (score > threshold)
                    yield return (targetWordIndex, score);
            }
        }

        public double GetTranslationScore(string sourceWord, string targetWord)
        {
            CheckDisposed();

            double dirScore = _directWordAlignmentEngine.GetTranslationScore(sourceWord, targetWord);
            double invScore = _inverseWordAlignmentEngine.GetTranslationScore(targetWord, sourceWord);
            return Math.Max(dirScore, invScore);
        }

        public double GetTranslationScore(int sourceWordIndex, int targetWordIndex)
        {
            CheckDisposed();

            double dirScore = _directWordAlignmentEngine.GetTranslationScore(sourceWordIndex, targetWordIndex);
            double invScore = _inverseWordAlignmentEngine.GetTranslationScore(targetWordIndex, sourceWordIndex);
            return Math.Max(dirScore, invScore);
        }

        public IReadOnlyCollection<AlignedWordPair> GetBestAlignedWordPairs(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment
        )
        {
            CheckDisposed();

            WordAlignmentMatrix matrix = Align(sourceSegment, targetSegment);
            IReadOnlyCollection<AlignedWordPair> wordPairs = matrix.ToAlignedWordPairs();
            ComputeAlignedWordPairScores(sourceSegment, targetSegment, wordPairs);
            return wordPairs;
        }

        public void ComputeAlignedWordPairScores(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            IReadOnlyCollection<AlignedWordPair> wordPairs
        )
        {
            AlignedWordPair[] inverseWordPairs = wordPairs.Select(wp => wp.Invert()).ToArray();
            _directWordAlignmentEngine.ComputeAlignedWordPairScores(sourceSegment, targetSegment, wordPairs);
            _inverseWordAlignmentEngine.ComputeAlignedWordPairScores(targetSegment, sourceSegment, inverseWordPairs);
            foreach (var (wordPair, inverseWordPair) in wordPairs.Zip(inverseWordPairs, (wp, invWp) => (wp, invWp)))
            {
                wordPair.TranslationScore = Math.Max(wordPair.TranslationScore, inverseWordPair.TranslationScore);
                wordPair.AlignmentScore = Math.Max(wordPair.AlignmentScore, inverseWordPair.AlignmentScore);
            }
        }

        protected override void DisposeManagedResources()
        {
            _directWordAlignmentEngine.Dispose();
            _inverseWordAlignmentEngine.Dispose();
        }
    }
}
