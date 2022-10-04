using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
    public interface ITranslationEngine : IDisposable
    {
        TranslationResult Translate(IReadOnlyList<string> segment);

        IReadOnlyList<TranslationResult> Translate(int n, IReadOnlyList<string> segment);

        IReadOnlyList<TranslationResult> TranslateBatch(IReadOnlyList<IReadOnlyList<string>> segments);

        IReadOnlyList<IReadOnlyList<TranslationResult>> TranslateBatch(
            int n,
            IReadOnlyList<IReadOnlyList<string>> segments
        );
    }
}
