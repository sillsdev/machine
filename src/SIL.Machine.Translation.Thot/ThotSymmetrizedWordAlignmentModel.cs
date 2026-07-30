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

        public int TrainingAlignmentCount => _directWordAlignmentModel.TrainingAlignmentCount;

        public WordAlignmentMatrix GetTrainingAlignment(int n)
        {
            WordAlignmentMatrix bestMatrix = _directWordAlignmentModel.GetTrainingAlignment(n);
            if (Heuristic == SymmetrizationHeuristic.None)
                return bestMatrix;

            WordAlignmentMatrix invMatrix = _inverseWordAlignmentModel.GetTrainingAlignment(n);
            invMatrix.Transpose();

            // Skip the combine when the matrices are degenerate or their dimensions don't
            // line up (e.g. an out-of-range n, or a pair filtered out of training in only
            // one direction): the heuristic operations require matching dimensions.
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
