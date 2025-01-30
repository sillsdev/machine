using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
    public interface IWordAlignmentModel : IWordAlignmentEngine
    {
        ITrainer CreateTrainer(IParallelTextCorpus corpus);
        Task SaveAsync(CancellationToken cancellationToken = default);
        void Save();
    }
}
