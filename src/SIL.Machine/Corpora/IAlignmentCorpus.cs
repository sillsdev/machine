using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface IAlignmentCorpus : IEnumerable<AlignmentRow>
	{
		IEnumerable<IAlignmentCollection> AlignmentCollections { get; }

		IAlignmentCollection this[string id] { get; }

		bool TryGetAlignmentCollection(string id, out IAlignmentCollection alignmentCollection);
	}
}
