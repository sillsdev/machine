using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.Translation
{
    public interface IInteractiveTranslationEngine : ITranslationEngine, IWordAlignerEngine
    {
        Task<WordGraph> GetWordGraphAsync(string segment, CancellationToken cancellationToken = default);

        Task TrainSegmentAsync(
            string sourceSegment,
            string targetSegment,
            bool sentenceStart = true,
            CancellationToken cancellationToken = default
        );

        Task<WordGraph> GetWordGraphAsync(IReadOnlyList<string> segment, CancellationToken cancellationToken = default);

        Task TrainSegmentAsync(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            bool sentenceStart = true,
            CancellationToken cancellationToken = default
        );

        WordGraph GetWordGraph(string segment);

        void TrainSegment(string sourceSegment, string targetSegment, bool sentenceStart = true);

        WordGraph GetWordGraph(IReadOnlyList<string> segment);

        void TrainSegment(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            bool sentenceStart = true
        );
    }
}
