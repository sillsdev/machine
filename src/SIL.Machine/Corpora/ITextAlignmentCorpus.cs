using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ITextAlignmentCorpus
	{
		IEnumerable<ITextAlignmentCollection> TextAlignmentCollections { get; }

		ITextAlignmentCollection this[string id] { get; }

		ITextAlignmentCorpus Invert();

		ITextAlignmentCollection CreateNullTextAlignmentCollection(string id);
	}
}
