using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
    public class SymmetrizedWordAlignmentModel : SymmetrizedWordAlignmentEngine, IWordAlignmentModel
    {
        private readonly IWordAlignmentModel _directWordAlignmentModel;
        private readonly IWordAlignmentModel _inverseWordAlignmentModel;

        public SymmetrizedWordAlignmentModel(
            IWordAlignmentModel directWordAlignmentModel,
            IWordAlignmentModel inverseWordAlignmentModel
        )
            : base(directWordAlignmentModel, inverseWordAlignmentModel)
        {
            _directWordAlignmentModel = directWordAlignmentModel;
            _inverseWordAlignmentModel = inverseWordAlignmentModel;
        }

        public ITrainer CreateTrainer(IParallelTextCorpus corpus)
        {
            CheckDisposed();

            ITrainer directTrainer = _directWordAlignmentModel.CreateTrainer(corpus);
            ITrainer inverseTrainer = _inverseWordAlignmentModel.CreateTrainer(corpus.Invert());

            return new SymmetrizedWordAlignmentModelTrainer(directTrainer, inverseTrainer);
        }

        protected override void DisposeManagedResources()
        {
            _directWordAlignmentModel.Dispose();
            _inverseWordAlignmentModel.Dispose();
        }
    }
}
