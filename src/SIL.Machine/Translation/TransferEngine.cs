using SIL.Machine.Annotations;
using SIL.Machine.Corpora;
using SIL.Machine.Morphology;
using SIL.ObjectModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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

        public TranslationResult Translate(IReadOnlyList<string> segment)
        {
            CheckDisposed();

            return Translate(1, segment)[0];
        }

        public IReadOnlyList<TranslationResult> Translate(int n, IReadOnlyList<string> segment)
        {
            CheckDisposed();

            IEnumerable<IEnumerable<WordAnalysis>> sourceAnalyses = segment.Select(
                word => _sourceAnalyzer.AnalyzeWord(word)
            );

            return _transferer
                .Transfer(sourceAnalyses)
                .Take(n)
                .Select(
                    transferResult =>
                    {
                        IReadOnlyList<WordAnalysis> targetAnalyses = transferResult.TargetAnalyses;
                        WordAlignmentMatrix waMatrix = transferResult.WordAlignmentMatrix;

                        var translation = new List<string>();
                        var confidences = new List<double>();
                        var sources = new List<TranslationSources>();
                        var alignment = new WordAlignmentMatrix(segment.Count, targetAnalyses.Count);
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
                                translation.Add(targetWord);
                                confidences.Add(wordConfidence);
                                sources.Add(source);
                                confidence = Math.Min(confidence, wordConfidence);
                            }
                        }

                        return new TranslationResult(
                            segment.Count,
                            translation,
                            confidences,
                            sources,
                            alignment,
                            new[] { new Phrase(Range<int>.Create(0, segment.Count), translation.Count, confidence) }
                        );
                    }
                )
                .ToArray();
        }

        public IEnumerable<TranslationResult> TranslateBatch(
            IEnumerable<IReadOnlyList<string>> segments,
            int? batchSize = null
        )
        {
            CheckDisposed();

            if (batchSize == null)
                return segments.AsParallel().AsOrdered().Select(segment => Translate(segment));

            return Partitioner
                .Create(segments.Batch((int)batchSize), EnumerablePartitionerOptions.NoBuffering)
                .AsParallel()
                .AsOrdered()
                .Select(batch => batch.Select(segment => Translate(segment)))
                .SelectMany(batch => batch);
        }

        public IEnumerable<IReadOnlyList<TranslationResult>> TranslateBatch(
            int n,
            IEnumerable<IReadOnlyList<string>> segments,
            int? batchSize = null
        )
        {
            CheckDisposed();

            if (batchSize == null)
                return segments.AsParallel().AsOrdered().Select(segment => Translate(n, segment));

            return Partitioner
                .Create(segments.Batch((int)batchSize), EnumerablePartitionerOptions.NoBuffering)
                .AsParallel()
                .AsOrdered()
                .Select(batch => batch.Select(segment => Translate(n, segment)))
                .SelectMany(batch => batch);
        }
    }
}
