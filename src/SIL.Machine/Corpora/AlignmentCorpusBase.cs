using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public abstract class AlignmentCorpusBase : IAlignmentCorpus
    {
        public abstract IEnumerable<IAlignmentCollection> AlignmentCollections { get; }

        public virtual int Count(bool includeEmpty = true, IEnumerable<string> textIds = null)
        {
            return includeEmpty ? GetRows(textIds).Count() : GetRows(textIds).Count(r => !r.IsEmpty);
        }

        int ICorpus<AlignmentRow>.Count(bool includeEmpty)
        {
            return Count(includeEmpty, null);
        }

        public IEnumerable<AlignmentRow> GetRows()
        {
            return GetRows(null);
        }

        public abstract IEnumerable<AlignmentRow> GetRows(IEnumerable<string> textIds);

        public IEnumerator<AlignmentRow> GetEnumerator()
        {
            return GetRows().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
