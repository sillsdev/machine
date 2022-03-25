using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ITextCorpus : IEnumerable<TextRow>
	{
		IEnumerable<IText> Texts { get; }

		IEnumerable<TextRow> GetRows(IEnumerable<string> textIds = null);
	}
}
