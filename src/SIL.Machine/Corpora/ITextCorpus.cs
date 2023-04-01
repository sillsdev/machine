using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
    public interface ITextCorpus : ICorpus<TextRow>
    {
        IEnumerable<IText> Texts { get; }

        IEnumerable<TextRow> GetRows(IEnumerable<string> textIds);

        bool IsTokenized { get; }
    }
}
