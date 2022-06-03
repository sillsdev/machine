using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
    public abstract class TextCorpusBase : CorpusBase<TextRow>, ITextCorpus
    {
        public abstract IEnumerable<IText> Texts { get; }

        public override IEnumerable<TextRow> GetRows()
        {
            return GetRows(null);
        }

        public abstract IEnumerable<TextRow> GetRows(IEnumerable<string> textIds);
    }
}
