using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
    public interface ICorpus<T> : IEnumerable<T>
        where T : IRow
    {
        int Count(bool includeEmpty = true);

        IEnumerable<T> GetRows();
    }
}
