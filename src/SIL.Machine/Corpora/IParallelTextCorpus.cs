using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
    public interface IParallelTextCorpus : ICorpus<ParallelTextRow>
    {
        bool IsSourceTokenized { get; }
        bool IsTargetTokenized { get; }

        int Count(bool includeEmpty = true, IEnumerable<string> textIds = null);

        IEnumerable<ParallelTextRow> GetRows(IEnumerable<string> textIds);
    }
}
