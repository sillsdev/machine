using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ITextAlignmentCorpus : ITextAlignmentCorpusView
	{
		IEnumerable<ITextAlignmentCollection> TextAlignmentCollections { get; }

		ITextAlignmentCollection this[string id] { get; }

		bool TryGetTextAlignmentCollection(string id, out ITextAlignmentCollection textAlignmentCollection);
	}
}
