using System.Collections.Generic;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public abstract class TextCorpusBase : CorpusBase<TextRow>, ITextCorpus
    {
        public abstract IEnumerable<IText> Texts { get; }
        public abstract bool IsTokenized { get; }
        public abstract ScrVers Versification { get; }

        public override IEnumerable<TextRow> GetRows()
        {
            return GetRows(null);
        }

        public abstract IEnumerable<TextRow> GetRows(IEnumerable<string> textIds);
    }
}
