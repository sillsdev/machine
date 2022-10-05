using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.Translation
{
    public interface IInteractiveTranslationEngine : ITranslationEngine
    {
        Task<WordGraph> GetWordGraphAsync(IReadOnlyList<string> segment, CancellationToken cancellationToken = default);

        Task TrainSegmentAsync(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            bool sentenceStart = true,
            CancellationToken cancellationToken = default
        );

        WordGraph GetWordGraph(IReadOnlyList<string> segment);

        void TrainSegment(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            bool sentenceStart = true
        );
    }
}
