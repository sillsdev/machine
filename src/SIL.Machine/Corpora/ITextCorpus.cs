using System.Collections.Generic;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public interface ITextCorpus : ICorpus<TextRow>
    {
        IEnumerable<IText> Texts { get; }

        bool IsTokenized { get; }

        ScrVers Versification { get; }

        int Count(bool includeEmpty = true, IEnumerable<string> textIds = null);

        IEnumerable<TextRow> GetRows(IEnumerable<string> textIds);
    }
}
