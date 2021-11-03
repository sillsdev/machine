using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ITextCorpus
	{
		IEnumerable<IText> Texts { get; }

		IText this[string id] { get; }

		IText CreateNullText(string id);
	}
}
