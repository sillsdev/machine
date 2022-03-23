using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ITextCorpus : IEnumerable<TextRow>
	{
		IEnumerable<IText> Texts { get; }

		IText this[string id] { get; }

		bool TryGetText(string id, out IText text);
	}
}
