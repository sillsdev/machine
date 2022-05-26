using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
    public interface IAlignmentCorpus : ICorpus<AlignmentRow>
    {
        IEnumerable<IAlignmentCollection> AlignmentCollections { get; }

        IEnumerable<AlignmentRow> GetRows(IEnumerable<string> alignmentCollectionIds);
    }
}
