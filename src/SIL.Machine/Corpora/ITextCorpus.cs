using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ITextCorpus
	{
		IEnumerable<IText> Texts { get; }

		bool TryGetText(string id, out IText text);

		IText GetText(string id);
	}
}
