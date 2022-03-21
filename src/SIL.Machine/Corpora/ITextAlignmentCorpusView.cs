using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ITextAlignmentCorpusView
	{
		ITextAlignmentCorpus Source { get; }
		IEnumerable<TextAlignmentCorpusRow> GetRows();
	}
}
