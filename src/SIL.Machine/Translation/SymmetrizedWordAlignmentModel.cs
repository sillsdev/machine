using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
    public class SymmetrizedWordAlignmentModel : DisposableBase, IWordAlignmentModel
    {
        private readonly IWordAlignmentModel _directWordAlignmentModel;
        private readonly IWordAlignmentModel _inverseWordAlignmentModel;
        private readonly SymmetrizedWordAligner _aligner;

        public SymmetrizedWordAlignmentModel(
            IWordAlignmentModel directWordAlignmentModel,
            IWordAlignmentModel inverseWordAlignmentModel
        )
        {
            _directWordAlignmentModel = directWordAlignmentModel;
            _inverseWordAlignmentModel = inverseWordAlignmentModel;
            _aligner = new SymmetrizedWordAligner(DirectWordAlignmentEngine, InverseWordAlignmentEngine);
        }

        public ITrainer CreateTrainer(IParallelTextCorpus corpus)
        {
            CheckDisposed();

            ITrainer directTrainer = _directWordAlignmentModel.CreateTrainer(corpus);
            ITrainer inverseTrainer = _inverseWordAlignmentModel.CreateTrainer(corpus.Invert());

            return new SymmetrizedWordAlignmentModelTrainer(directTrainer, inverseTrainer);
        }

        public SymmetrizationHeuristic Heuristic
        {
            get => _aligner.Heuristic;
            set => _aligner.Heuristic = value;
        }

        public IWordAligner DirectWordAlignmentEngine
        {
            get
            {
                CheckDisposed();

                return _directWordAlignmentModel;
            }
        }

        public IWordAligner InverseWordAlignmentEngine
        {
            get
            {
                CheckDisposed();

                return _inverseWordAlignmentModel;
            }
        }

        public IWordVocabulary SourceWords
        {
            get
            {
                CheckDisposed();

                return _directWordAlignmentModel.SourceWords;
            }
        }

        public IWordVocabulary TargetWords
        {
            get
            {
                CheckDisposed();

                return _directWordAlignmentModel.TargetWords;
            }
        }

        public IReadOnlySet<int> SpecialSymbolIndices => _directWordAlignmentModel.SpecialSymbolIndices;

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

            foreach ((string targetWord, double dirScore) in _directWordAlignmentModel.GetTranslations(sourceWord))
            {
                double invScore = _inverseWordAlignmentModel.GetTranslationScore(targetWord, sourceWord);
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
                (int targetWordIndex, double dirScore) in _directWordAlignmentModel.GetTranslations(sourceWordIndex)
            )
            {
                double invScore = _inverseWordAlignmentModel.GetTranslationScore(targetWordIndex, sourceWordIndex);
                double score = Math.Max(dirScore, invScore);
                if (score > threshold)
                    yield return (targetWordIndex, score);
            }
        }

        public double GetTranslationScore(string sourceWord, string targetWord)
        {
            CheckDisposed();

            double dirScore = _directWordAlignmentModel.GetTranslationScore(sourceWord, targetWord);
            double invScore = _inverseWordAlignmentModel.GetTranslationScore(targetWord, sourceWord);
            return Math.Max(dirScore, invScore);
        }

        public double GetTranslationScore(int sourceWordIndex, int targetWordIndex)
        {
            CheckDisposed();

            double dirScore = _directWordAlignmentModel.GetTranslationScore(sourceWordIndex, targetWordIndex);
            double invScore = _inverseWordAlignmentModel.GetTranslationScore(targetWordIndex, sourceWordIndex);
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
            _directWordAlignmentModel.ComputeAlignedWordPairScores(sourceSegment, targetSegment, wordPairs);
            _inverseWordAlignmentModel.ComputeAlignedWordPairScores(targetSegment, sourceSegment, inverseWordPairs);
            foreach (var (wordPair, inverseWordPair) in wordPairs.Zip(inverseWordPairs, (wp, invWp) => (wp, invWp)))
            {
                wordPair.TranslationScore = Math.Max(wordPair.TranslationScore, inverseWordPair.TranslationScore);
                wordPair.AlignmentScore = Math.Max(wordPair.AlignmentScore, inverseWordPair.AlignmentScore);
            }
        }

        protected override void DisposeManagedResources()
        {
            _directWordAlignmentModel.Dispose();
            _inverseWordAlignmentModel.Dispose();
        }
    }
}
