using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
    public interface ITranslationEngine : IDisposable
    {
        TranslationResult Translate(IReadOnlyList<string> segment);

        IReadOnlyList<TranslationResult> Translate(int n, IReadOnlyList<string> segment);

        IEnumerable<TranslationResult> TranslateBatch(
            IEnumerable<IReadOnlyList<string>> segments,
            int? batchSize = null
        );

        IEnumerable<IReadOnlyList<TranslationResult>> TranslateBatch(
            int n,
            IEnumerable<IReadOnlyList<string>> segments,
            int? batchSize = null
        );
    }
}
