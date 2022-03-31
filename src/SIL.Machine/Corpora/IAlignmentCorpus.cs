using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface IAlignmentCorpus : IEnumerable<AlignmentRow>
	{
		IEnumerable<IAlignmentCollection> AlignmentCollections { get; }

		bool MissingRowsAllowed { get; }

		int Count(bool includeEmpty = true);

		IEnumerable<AlignmentRow> GetRows(IEnumerable<string> alignmentCollectionIds = null);
	}
}
