using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
    public interface IAlignmentCorpus : ICorpus<AlignmentRow>
    {
        IEnumerable<IAlignmentCollection> AlignmentCollections { get; }

        int Count(bool includeEmpty = true, IEnumerable<string> textIds = null);

        IEnumerable<AlignmentRow> GetRows(IEnumerable<string> textIds);
    }
}
