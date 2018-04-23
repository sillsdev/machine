using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ITextAlignmentCorpus
	{
		IEnumerable<ITextAlignmentCollection> TextAlignmentCollections { get; }

		bool TryGetTextAlignmentCollection(string id, out ITextAlignmentCollection alignments);

		ITextAlignmentCollection GetTextAlignmentCollection(string id);

		ITextAlignmentCorpus Invert();
	}
}
