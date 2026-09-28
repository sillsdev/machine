using System;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot
{
    public class ThotSymmetrizedWordAlignmentModel : SymmetrizedWordAlignmentModel, ITransductiveWordAlignmentModel
    {
        private readonly ThotWordAlignmentModel _directWordAlignmentModel;
        private readonly ThotWordAlignmentModel _inverseWordAlignmentModel;

        public ThotSymmetrizedWordAlignmentModel(
            ThotWordAlignmentModel directWordAlignmentModel,
            ThotWordAlignmentModel inverseWordAlignmentModel
        )
            : base(directWordAlignmentModel, inverseWordAlignmentModel)
        {
            _directWordAlignmentModel = directWordAlignmentModel;
            _inverseWordAlignmentModel = inverseWordAlignmentModel;
        }

        public bool EmitTrainingAlignments
        {
            get => _directWordAlignmentModel.EmitTrainingAlignments;
            set
            {
                _directWordAlignmentModel.EmitTrainingAlignments = value;
                _inverseWordAlignmentModel.EmitTrainingAlignments = value;
            }
        }

        public int TrainingAlignmentCount =>
            Math.Min(
                _directWordAlignmentModel.TrainingAlignmentCount,
                _inverseWordAlignmentModel.TrainingAlignmentCount
            );

        public static ThotSymmetrizedWordAlignmentModel Create(ThotWordAlignmentModelType modelType) =>
            new ThotSymmetrizedWordAlignmentModel(
                ThotWordAlignmentModel.Create(modelType),
                ThotWordAlignmentModel.Create(modelType)
            );

        protected override ITrainer CreateTrainerCore(IParallelTextCorpus corpus) => CreateTrainer(corpus);

        public new ThotSymmetrizedWordAlignmentModelTrainer CreateTrainer(IParallelTextCorpus corpus)
        {
            CheckDisposed();

            return new ThotSymmetrizedWordAlignmentModelTrainer(
                _directWordAlignmentModel.CreateTrainer(corpus),
                _inverseWordAlignmentModel.CreateTrainer(corpus.Invert())
            );
        }

        public WordAlignmentMatrix GetTrainingAlignment(int n)
        {
            // Checked here as well, since either direction can hold more than the shared range.
            if (n < 0 || n >= TrainingAlignmentCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(n),
                    n,
                    $"The index must be less than the number of retained training alignments ({TrainingAlignmentCount})."
                );
            }

            WordAlignmentMatrix bestMatrix = _directWordAlignmentModel.GetTrainingAlignment(n);
            if (Heuristic == SymmetrizationHeuristic.None)
                return bestMatrix;

            WordAlignmentMatrix invMatrix = _inverseWordAlignmentModel.GetTrainingAlignment(n);
            invMatrix.Transpose();

            // Skip the combine when the matrices are degenerate or their dimensions don't line up
            // (a pair filtered out of training in only one direction): the heuristic operations
            // require matching dimensions.
            if (
                bestMatrix.RowCount == 0
                || bestMatrix.ColumnCount == 0
                || invMatrix.RowCount != bestMatrix.RowCount
                || invMatrix.ColumnCount != bestMatrix.ColumnCount
            )
            {
                return bestMatrix;
            }

            bestMatrix.SymmetrizeWith(invMatrix, Heuristic);
            return bestMatrix;
        }
    }
}
