using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public abstract class NParallelTextCorpusBase : INParallelTextCorpus
    {
        int ICorpus<NParallelTextRow>.Count(bool includeEmpty)
        {
            return Count(includeEmpty, null);
        }

        public virtual int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
        {
            return includeEmpty ? GetRows(textIds).Count() : GetRows(textIds).Count(r => !r.IsEmpty);
        }

        public IEnumerable<NParallelTextRow> GetRows()
        {
            return GetRows(null);
        }

        public abstract IEnumerable<NParallelTextRow> GetRows(IEnumerable<string> textIds);

        public IEnumerator<NParallelTextRow> GetEnumerator()
        {
            return GetRows().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
