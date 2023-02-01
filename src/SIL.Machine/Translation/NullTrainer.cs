using System;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
    public class NullTrainer : DisposableBase, ITrainer
    {
        public TrainStats Stats { get; } = new TrainStats();

        public Task TrainAsync(IProgress<ProgressStatus> progress = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SaveAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
