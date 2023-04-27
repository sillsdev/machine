using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
    public interface ITruecaser
    {
        ITrainer CreateTrainer(ITextCorpus corpus);

        void TrainSegment(IReadOnlyList<string> segment, bool sentenceStart = true);

        IReadOnlyList<string> Truecase(IReadOnlyList<string> segment);

        Task SaveAsync(CancellationToken cancellationToken = default);
        void Save();
    }
}
