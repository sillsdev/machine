using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface IParallelTextCorpusView
	{
		IEnumerable<ParallelTextCorpusRow> GetRows(bool allSourceRows = false, bool allTargetRows = false);
	}
}
