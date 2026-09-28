using System;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
    public class ThotSymmetrizedWordAlignmentModelTrainer : DisposableBase, ITrainer
    {
        private readonly ThotWordAlignmentModelTrainer _directTrainer;
        private readonly ThotWordAlignmentModelTrainer _inverseTrainer;

        internal ThotSymmetrizedWordAlignmentModelTrainer(
            ThotWordAlignmentModelTrainer directTrainer,
            ThotWordAlignmentModelTrainer inverseTrainer
        )
        {
            _directTrainer = directTrainer;
            _inverseTrainer = inverseTrainer;
        }

        public TrainStats Stats => _directTrainer.Stats;

        public void Train(IProgress<ProgressStatus> progress = null, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            var reporter = new PhasedProgressReporter(
                progress,
                new Phase("Training direct alignment model"),
                new Phase("Training inverse alignment model")
            );

            using (PhaseProgress phaseProgress = reporter.StartNextPhase())
                _directTrainer.Train(phaseProgress, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            using (PhaseProgress phaseProgress = reporter.StartNextPhase())
                _inverseTrainer.Train(phaseProgress, cancellationToken);
        }

        public Task TrainAsync(IProgress<ProgressStatus> progress = null, CancellationToken cancellationToken = default)
        {
            Train(progress, cancellationToken);
            return Task.CompletedTask;
        }

        public void Save()
        {
            CheckDisposed();

            _directTrainer.Save();
            _inverseTrainer.Save();
        }

        public Task SaveAsync(CancellationToken cancellationToken = default)
        {
            Save();
            return Task.CompletedTask;
        }

        protected override void DisposeManagedResources()
        {
            _directTrainer.Dispose();
            _inverseTrainer.Dispose();
        }
    }
}
