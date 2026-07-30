using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation.Thot
{
    public static class IParallelTextCorpusExtensions
    {
        public static async Task<IParallelTextCorpus> WordAlignAsync(
            this IParallelTextCorpus corpus,
            ThotWordAlignmentModelType modelType,
            int batchSize = 1024,
            SymmetrizationHeuristic symmetrizationHeuristic = SymmetrizationHeuristic.GrowDiagFinalAnd,
            IProgress<ProgressStatus> progress = null
        )
        {
            var model = new ThotSymmetrizedWordAlignmentModel(
                ThotWordAlignmentModel.Create(modelType),
                ThotWordAlignmentModel.Create(modelType)
            )
            {
                Heuristic = symmetrizationHeuristic,
                // Retain the alignments computed during training so that the corpus can be aligned
                // without a separate, potentially expensive, inference pass.
                EmitTrainingAlignments = true,
            };

            using (ITrainer trainer = model.CreateTrainer(corpus))
            {
                await trainer.TrainAsync(progress);
                await trainer.SaveAsync();
            }

            return corpus.WordAlign(model, batchSize);
        }

        public static IParallelTextCorpus WordAlign(
            this IParallelTextCorpus corpus,
            ThotSymmetrizedWordAlignmentModel model,
            int batchSize = 1024
        )
        {
            if (model.EmitTrainingAlignments)
                return new TransductiveWordAlignParallelTextCorpus(corpus, model);

            return CorporaExtensions.WordAlign(corpus, model, batchSize);
        }

        private class TransductiveWordAlignParallelTextCorpus : ParallelTextCorpusBase
        {
            private readonly IParallelTextCorpus _corpus;
            private readonly ITransductiveWordAlignmentModel _model;

            public TransductiveWordAlignParallelTextCorpus(
                IParallelTextCorpus corpus,
                ITransductiveWordAlignmentModel model
            )
            {
                _corpus = corpus;
                _model = model;
            }

            public override bool IsSourceTokenized => _corpus.IsSourceTokenized;

            public override bool IsTargetTokenized => _corpus.IsTargetTokenized;

            public override IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds)
            {
                // The training alignments are keyed by the order in which the sentence pairs were added
                // during training, so the full corpus must be iterated to keep the index in sync; rows that
                // are not in the requested texts are skipped rather than filtered out of the enumeration.
                var textIdList = textIds?.ToList();
                List<ParallelTextRow> rows = _corpus.GetRows().ToList();
                for (int i = 0; i < rows.Count; i++)
                {
                    ParallelTextRow row = rows[i];
                    if (textIdList != null && !textIdList.Contains(row.TextId))
                        continue;

                    WordAlignmentMatrix alignment = _model.GetTrainingAlignment(i);
                    WordAlignmentMatrix knownAlignment = row.CreateAlignmentMatrix();
                    if (knownAlignment != null)
                    {
                        knownAlignment.PrioritySymmetrizeWith(alignment);
                        alignment = knownAlignment;
                    }

                    IReadOnlyCollection<AlignedWordPair> wordPairs = alignment.ToAlignedWordPairs();
                    if (_model is IWordAlignmentModel wordAlignmentModel)
                    {
                        wordAlignmentModel.ComputeAlignedWordPairScores(
                            row.SourceSegment,
                            row.TargetSegment,
                            wordPairs
                        );
                    }

                    row.AlignedWordPairs = wordPairs;
                    yield return row;
                }
            }
        }
    }
}
