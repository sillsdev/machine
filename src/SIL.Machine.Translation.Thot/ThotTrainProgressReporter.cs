using System;
using System.Threading;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation.Thot
{
    public class ThotTrainProgressReporter : PhasedProgressReporter
    {
        private readonly static Phase[] TrainPhases =
        {
            new Phase("Training language model", 0.01),
            new Phase("Training direct alignment model", 0.2),
            new Phase("Generating best direct alignments", reportSteps: false),
            new Phase("Training inverse alignment model", 0.2),
            new Phase("Generating best inverse alignments", reportSteps: false),
            new Phase("Merging alignments"),
            new Phase("Generating phrase table"),
            new Phase("Tuning language model"),
            new Phase("Tuning translation model", 0.4, reportSteps: false),
            new Phase("Finalizing", 0.05, reportSteps: false)
        };

        private readonly CancellationToken _cancellationToken;

        public ThotTrainProgressReporter(IProgress<ProgressStatus> progress, CancellationToken cancellationToken)
            : base(progress, TrainPhases)
        {
            _cancellationToken = cancellationToken;
        }

        public void CheckCanceled()
        {
            _cancellationToken.ThrowIfCancellationRequested();
        }

        public override PhaseProgress StartNextPhase()
        {
            CheckCanceled();

            return base.StartNextPhase();
        }

        protected override void Report(ProgressStatus value)
        {
            CheckCanceled();

            base.Report(value);
        }
    }
}
