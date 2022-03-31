using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ITextCorpus : IEnumerable<TextRow>
	{
		IEnumerable<IText> Texts { get; }

		bool MissingRowsAllowed { get; }

		int Count(bool includeEmpty = true);

		IEnumerable<TextRow> GetRows(IEnumerable<string> textIds = null);
	}
}
