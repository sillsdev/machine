using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation.Thot
{
    public static class IParallelTextCorpusExtensions
    {
        public static IParallelTextCorpus WordAlign(
            this IParallelTextCorpus corpus,
            ThotWordAlignmentModelType modelType = ThotWordAlignmentModelType.FastAlign,
            SymmetrizationHeuristic symmetrizationHeuristic = SymmetrizationHeuristic.GrowDiagFinalAnd,
            IProgress<ProgressStatus> progress = null
        ) => new TrainedWordAlignParallelTextCorpus(corpus, modelType, symmetrizationHeuristic, progress);

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

        private class TransductiveWordAlignParallelTextCorpus : WordAlignParallelTextCorpusBase
        {
            private readonly IParallelTextCorpus _corpus;
            private readonly ITransductiveWordAlignmentModel _model;

            public TransductiveWordAlignParallelTextCorpus(
                IParallelTextCorpus corpus,
                ITransductiveWordAlignmentModel model
            )
                : base(corpus)
            {
                _corpus = corpus;
                _model = model;
            }

            public override IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds) =>
                GetTransductiveRows(_corpus, _model, textIds);
        }

        private class TrainedWordAlignParallelTextCorpus : WordAlignParallelTextCorpusBase
        {
            private readonly IParallelTextCorpus _corpus;
            private readonly ThotWordAlignmentModelType _modelType;
            private readonly SymmetrizationHeuristic _symmetrizationHeuristic;
            private readonly IProgress<ProgressStatus> _progress;

            public TrainedWordAlignParallelTextCorpus(
                IParallelTextCorpus corpus,
                ThotWordAlignmentModelType modelType,
                SymmetrizationHeuristic symmetrizationHeuristic,
                IProgress<ProgressStatus> progress
            )
                : base(corpus)
            {
                _corpus = corpus;
                _modelType = modelType;
                _symmetrizationHeuristic = symmetrizationHeuristic;
                _progress = progress;
            }

            public override IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds)
            {
                // Training on only the requested texts keeps the training-alignment index in sync with the rows.
                IParallelTextCorpus corpus = _corpus.FilterTexts(textIds);
                // Training in the generator ties the model's lifetime to reading the rows, at the cost of
                // training a new model on each iteration.
                using (var model = ThotSymmetrizedWordAlignmentModel.Create(_modelType))
                {
                    model.Heuristic = _symmetrizationHeuristic;
                    // Retain the alignments computed during training so that the corpus can be aligned
                    // without a separate, potentially expensive, inference pass.
                    model.EmitTrainingAlignments = true;
                    using (ITrainer trainer = model.CreateTrainer(corpus))
                    {
                        trainer.TrainAsync(_progress).GetAwaiter().GetResult();
                        trainer.SaveAsync().GetAwaiter().GetResult();
                    }

                    foreach (ParallelTextRow row in GetTransductiveRows(corpus, model, textIds: null))
                        yield return row;
                }
            }
        }

        private static IEnumerable<ParallelTextRow> GetTransductiveRows(
            IParallelTextCorpus corpus,
            ITransductiveWordAlignmentModel model,
            IEnumerable<string> textIds
        )
        {
            // The training alignments are keyed by the order in which the sentence pairs were added during
            // training, so the corpus the model was trained on must be iterated in full to keep the index in
            // sync; rows outside the requested texts are skipped rather than filtered out.
            var textIdList = textIds?.ToList();
            List<ParallelTextRow> rows = corpus.GetRows().ToList();
            for (int i = 0; i < rows.Count; i++)
            {
                ParallelTextRow row = rows[i];
                if (textIdList != null && !textIdList.Contains(row.TextId))
                    continue;

                WordAlignmentMatrix alignment = model.GetTrainingAlignment(i);
                WordAlignmentMatrix knownAlignment = row.CreateAlignmentMatrix();
                if (knownAlignment != null)
                {
                    knownAlignment.PrioritySymmetrizeWith(alignment);
                    alignment = knownAlignment;
                }

                IReadOnlyCollection<AlignedWordPair> wordPairs = alignment.ToAlignedWordPairs();
                if (model is IWordAlignmentModel wordAlignmentModel)
                {
                    wordAlignmentModel.ComputeAlignedWordPairScores(row.SourceSegment, row.TargetSegment, wordPairs);
                }

                row.AlignedWordPairs = wordPairs;
                yield return row;
            }
        }
    }
}
