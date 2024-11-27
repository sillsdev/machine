using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.Translation
{
    public interface IWordAlignmentEngine : IWordAligner, IDisposable
    {
        Task<WordAlignmentResult> GetBestAlignmentAsync(
            string sourceSegment,
            string targetSegment,
            CancellationToken cancellationToken = default
        );

        Task<WordAlignmentResult> GetBestAlignmentAsync(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            CancellationToken cancellationToken = default
        );

        WordAlignmentResult GetBestAlignment(string sourceSegment, string targetSegment);

        WordAlignmentResult GetBestAlignment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment);
    }
}
