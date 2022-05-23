using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
    public interface ITranslationEngine : IDisposable
    {
        TranslationResult Translate(IReadOnlyList<string> segment);

        IReadOnlyList<TranslationResult> Translate(int n, IReadOnlyList<string> segment);

        IEnumerable<TranslationResult> Translate(IEnumerable<IReadOnlyList<string>> segments);

        IEnumerable<IReadOnlyList<TranslationResult>> Translate(int n, IEnumerable<IReadOnlyList<string>> segments);
    }
}
