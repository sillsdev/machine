using System.Collections.Generic;

namespace SIL.Machine.Translation
{
    public interface IWordVocabulary : IReadOnlyList<string>
    {
        int IndexOf(string word);
    }
}
