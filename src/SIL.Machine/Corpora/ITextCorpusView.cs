using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ITextCorpusView
	{
		ITextCorpus Source { get; }
		IEnumerable<TextCorpusRow> GetRows(ITextCorpusView basedOn = null);
	}
}
