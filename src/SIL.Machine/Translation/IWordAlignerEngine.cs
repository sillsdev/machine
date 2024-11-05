using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.Translation
{
    public interface IWordAlignerEngine : IDisposable
    {
        Task<TranslationResult> GetBestPhraseAlignmentAsync(
            string sourceSegment,
            string targetSegment,
            CancellationToken cancellationToken = default
        );

        Task<TranslationResult> GetBestPhraseAlignmentAsync(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            CancellationToken cancellationToken = default
        );

        TranslationResult GetBestPhraseAlignment(string sourceSegment, string targetSegment);

        TranslationResult GetBestPhraseAlignment(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment
        );
    }
}
