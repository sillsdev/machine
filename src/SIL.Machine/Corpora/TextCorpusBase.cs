using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public abstract class TextCorpusBase : ITextCorpus
    {
        public abstract IEnumerable<IText> Texts { get; }
        public abstract bool IsTokenized { get; }
        public abstract ScrVers Versification { get; }

        int ICorpus<TextRow>.Count(bool includeEmpty)
        {
            return Count(includeEmpty, null);
        }

        public virtual int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
        {
            return includeEmpty ? GetRows(textIds).Count() : GetRows(textIds).Count(r => !r.IsEmpty);
        }

        public IEnumerable<TextRow> GetRows()
        {
            return GetRows(null);
        }

        public abstract IEnumerable<TextRow> GetRows(IEnumerable<string> textIds);

        public IEnumerator<TextRow> GetEnumerator()
        {
            return GetRows().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
