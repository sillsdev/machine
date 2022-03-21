using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ITextCorpus : ITextCorpusView
	{
		IEnumerable<IText> Texts { get; }

		IText this[string id] { get; }

		bool TryGetText(string id, out IText text);
	}
}
