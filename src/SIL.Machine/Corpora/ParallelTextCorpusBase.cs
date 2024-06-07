using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public abstract class ParallelTextCorpusBase : IParallelTextCorpus
    {
        public abstract bool IsSourceTokenized { get; }
        public abstract bool IsTargetTokenized { get; }

        int ICorpus<ParallelTextRow>.Count(bool includeEmpty)
        {
            return Count(includeEmpty, null);
        }

        public virtual int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
        {
            return includeEmpty ? GetRows(textIds).Count() : GetRows(textIds).Count(r => !r.IsEmpty);
        }

        public IEnumerable<ParallelTextRow> GetRows()
        {
            return GetRows(null);
        }

        public abstract IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds);

        public IEnumerator<ParallelTextRow> GetEnumerator()
        {
            return GetRows().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
