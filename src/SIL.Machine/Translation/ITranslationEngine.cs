using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.Translation
{
    public interface ITranslationEngine : IDisposable
    {
        Task<TranslationResult> TranslateAsync(string segment, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TranslationResult>> TranslateAsync(
            int n,
            string segment,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
            IReadOnlyList<string> segments,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<IReadOnlyList<TranslationResult>>> TranslateBatchAsync(
            int n,
            IReadOnlyList<string> segments,
            CancellationToken cancellationToken = default
        );

        Task<TranslationResult> TranslateAsync(
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<TranslationResult>> TranslateAsync(
            int n,
            IReadOnlyList<string> segment,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<TranslationResult>> TranslateBatchAsync(
            IReadOnlyList<IReadOnlyList<string>> segments,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<IReadOnlyList<TranslationResult>>> TranslateBatchAsync(
            int n,
            IReadOnlyList<IReadOnlyList<string>> segments,
            CancellationToken cancellationToken = default
        );

        TranslationResult Translate(string segment);

        IReadOnlyList<TranslationResult> Translate(int n, string segment);

        IReadOnlyList<TranslationResult> TranslateBatch(IReadOnlyList<string> segments);

        IReadOnlyList<IReadOnlyList<TranslationResult>> TranslateBatch(int n, IReadOnlyList<string> segments);

        TranslationResult Translate(IReadOnlyList<string> segment);

        IReadOnlyList<TranslationResult> Translate(int n, IReadOnlyList<string> segment);

        IReadOnlyList<TranslationResult> TranslateBatch(IReadOnlyList<IReadOnlyList<string>> segments);

        IReadOnlyList<IReadOnlyList<TranslationResult>> TranslateBatch(
            int n,
            IReadOnlyList<IReadOnlyList<string>> segments
        );
    }
}
