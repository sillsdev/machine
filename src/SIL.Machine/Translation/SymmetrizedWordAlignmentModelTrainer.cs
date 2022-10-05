using System;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
    public class SymmetrizedWordAlignmentModelTrainer : DisposableBase, ITrainer
    {
        private readonly ITrainer _directTrainer;
        private readonly ITrainer _inverseTrainer;

        public TrainStats Stats => _directTrainer.Stats;

        public SymmetrizedWordAlignmentModelTrainer(ITrainer directTrainer, ITrainer inverseTrainer)
        {
            _directTrainer = directTrainer;
            _inverseTrainer = inverseTrainer;
        }

        public async Task TrainAsync(
            IProgress<ProgressStatus> progress = null,
            CancellationToken cancellationToken = default
        )
        {
            CheckDisposed();

            var reporter = new PhasedProgressReporter(
                progress,
                new Phase("Training direct alignment model"),
                new Phase("Training inverse alignment model")
            );

            using (PhaseProgress phaseProgress = reporter.StartNextPhase())
                await _directTrainer.TrainAsync(phaseProgress, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            using (PhaseProgress phaseProgress = reporter.StartNextPhase())
                await _inverseTrainer.TrainAsync(phaseProgress, cancellationToken);
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            await _directTrainer.SaveAsync(cancellationToken);
            await _inverseTrainer.SaveAsync(cancellationToken);
        }

        protected override void DisposeManagedResources()
        {
            _directTrainer.Dispose();
            _inverseTrainer.Dispose();
        }
    }
}
