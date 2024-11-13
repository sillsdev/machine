using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
    public interface INParallelTextCorpus : ICorpus<NParallelTextRow>
    {
        int N { get; }
        IReadOnlyList<ITextCorpus> Corpora { get; }

        bool IsTokenized(int i);
        int Count(bool includeEmpty = true, IEnumerable<string> textIds = null);

        IEnumerable<NParallelTextRow> GetRows(IEnumerable<string> textIds);
    }
}
