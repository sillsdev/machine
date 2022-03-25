using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface IAlignmentCorpus : IEnumerable<AlignmentRow>
	{
		IEnumerable<IAlignmentCollection> AlignmentCollections { get; }

		IEnumerable<AlignmentRow> GetRows(IEnumerable<string> alignmentCollectionIds = null);
	}
}
