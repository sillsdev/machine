using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Annotations;
using SIL.Machine.Corpora;
using SIL.Machine.Morphology;
using SIL.Machine.Tokenization;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
    public class TransferEngine : DisposableBase, ITranslationEngine
    {
        private readonly IMorphologicalAnalyzer _sourceAnalyzer;
        private readonly ITransferer _transferer;
        private readonly IMorphologicalGenerator _targetGenerator;

        public TransferEngine(
            IMorphologicalAnalyzer sourceAnalyzer,
            ITransferer transferer,
            IMorphologicalGenerator targetGenerator
        )
        {
            _sourceAnalyzer = sourceAnalyzer;
            _transferer = transferer;
            _targetGenerator = targetGenerator;
        }

        public ITokenizer<string, int, string> SourceTokenizer { get; set; } = WhitespaceTokenizer.Instance;
        public IDetokenizer<string, string> TargetDetokenizer { get; set; } = WhitespaceDetokenizer.Instance;
        public bool LowercaseSource { get; set; }
        public ITruecaser Truecaser { get; set; }

        public Task<TranslationResult> TranslateAsync(string segment, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Translate(segment));
        }

        public Task<TranslationResult> TranslateAsync(
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Translate(segment));
        }

        public Task<IReadOnlyList<TranslationResult>> TranslateAsync(
            int n,
            string segment,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Translate(n, segment));
        }

        public Task<IReadOnlyList<TranslationResult>> TranslateAsync(
            int n,
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(Translate(n, segment));
        }

        public Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
            IReadOnlyList<string> segments,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(TranslateBatch(segments));
        }

        public Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
            IReadOnlyList<IReadOnlyList<string>> segments,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(TranslateBatch(segments));
        }

        public Task<IReadOnlyList<IReadOnlyList<TranslationResult>>> TranslateBatchAsync(
            int n,
            IReadOnlyList<string> segments,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(TranslateBatch(n, segments));
        }

        public Task<IReadOnlyList<IReadOnlyList<TranslationResult>>> TranslateBatchAsync(
            int n,
            IReadOnlyList<IReadOnlyList<string>> segments,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(TranslateBatch(n, segments));
        }

        public TranslationResult Translate(string segment)
        {
            CheckDisposed();

            return Translate(1, segment)[0];
        }

        public TranslationResult Translate(IReadOnlyList<string> segment)
        {
            CheckDisposed();

            return Translate(1, segment)[0];
        }

        public IReadOnlyList<TranslationResult> Translate(int n, string segment)
        {
            CheckDisposed();

            return Translate(n, SourceTokenizer.Tokenize(segment).ToArray());
        }

        public IReadOnlyList<TranslationResult> Translate(int n, IReadOnlyList<string> segment)
        {
            CheckDisposed();

            IReadOnlyList<string> normalizedSourceTokens = segment;
            if (LowercaseSource)
                normalizedSourceTokens = normalizedSourceTokens.Lowercase();

            IEnumerable<IEnumerable<WordAnalysis>> sourceAnalyses = normalizedSourceTokens.Select(
                word => _sourceAnalyzer.AnalyzeWord(word)
            );

            return _transferer
                .Transfer(sourceAnalyses)
                .Take(n)
                .Select(transferResult =>
                {
                    IReadOnlyList<WordAnalysis> targetAnalyses = transferResult.TargetAnalyses;
                    WordAlignmentMatrix waMatrix = transferResult.WordAlignmentMatrix;

                    var targetWords = new List<string>();
                    var confidences = new List<double>();
                    var sources = new List<TranslationSources>();
                    var alignment = new WordAlignmentMatrix(normalizedSourceTokens.Count, targetAnalyses.Count);
                    double confidence = double.MaxValue;
                    for (int j = 0; j < targetAnalyses.Count; j++)
                    {
                        int[] sourceIndices = Enumerable
                            .Range(0, waMatrix.RowCount)
                            .Where(i => waMatrix[i, j])
                            .ToArray();
                        string targetWord = targetAnalyses[j].IsEmpty
                            ? null
                            : _targetGenerator.GenerateWords(targetAnalyses[j]).FirstOrDefault();
                        double wordConfidence = 1.0;
                        TranslationSources source = TranslationSources.Transfer;
                        if (targetWord == null)
                        {
                            if (sourceIndices.Length > 0)
                            {
                                int i = sourceIndices[0];
                                targetWord = segment[i];
                                wordConfidence = 0;
                                source = TranslationSources.None;
                                alignment[i, j] = true;
                            }
                        }
                        else
                        {
                            foreach (int i in sourceIndices)
                                alignment[i, j] = true;
                        }

                        if (targetWord != null)
                        {
                            targetWords.Add(targetWord);
                            confidences.Add(wordConfidence);
                            sources.Add(source);
                            confidence = Math.Min(confidence, wordConfidence);
                        }
                    }

                    IReadOnlyList<string> targetTokens = targetWords;
                    if (Truecaser != null)
                        targetTokens = Truecaser.Truecase(targetTokens);
                    return new TranslationResult(
                        TargetDetokenizer.Detokenize(targetTokens),
                        segment,
                        targetTokens,
                        confidences,
                        sources,
                        alignment,
                        new[]
                        {
                            new Phrase(
                                Range<int>.Create(0, normalizedSourceTokens.Count),
                                targetWords.Count,
                                confidence
                            )
                        }
                    );
                })
                .ToArray();
        }

        public IReadOnlyList<TranslationResult> TranslateBatch(IReadOnlyList<string> segments)
        {
            CheckDisposed();

            return segments.AsParallel().AsOrdered().Select(segment => Translate(segment)).ToArray();
        }

        public IReadOnlyList<TranslationResult> TranslateBatch(IReadOnlyList<IReadOnlyList<string>> segments)
        {
            CheckDisposed();

            return segments.AsParallel().AsOrdered().Select(segment => Translate(segment)).ToArray();
        }

        public IReadOnlyList<IReadOnlyList<TranslationResult>> TranslateBatch(int n, IReadOnlyList<string> segments)
        {
            CheckDisposed();

            return segments.AsParallel().AsOrdered().Select(segment => Translate(n, segment)).ToArray();
        }

        public IReadOnlyList<IReadOnlyList<TranslationResult>> TranslateBatch(
            int n,
            IReadOnlyList<IReadOnlyList<string>> segments
        )
        {
            CheckDisposed();

            return segments.AsParallel().AsOrdered().Select(segment => Translate(n, segment)).ToArray();
        }
    }
}
