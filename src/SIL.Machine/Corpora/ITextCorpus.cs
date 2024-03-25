using System.Collections.Generic;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public interface ITextCorpus : ICorpus<TextRow>
    {
        IEnumerable<IText> Texts { get; }

        IEnumerable<TextRow> GetRows(IEnumerable<string> textIds);

        bool IsTokenized { get; }

        ScrVers Versification { get; }
    }
}
