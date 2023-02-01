using System;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation
{
    public interface ITrainer : IDisposable
    {
        Task TrainAsync(IProgress<ProgressStatus> progress = null, CancellationToken cancellationToken = default);
        Task SaveAsync(CancellationToken cancellationToken = default);

        TrainStats Stats { get; }
    }
}
