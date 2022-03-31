using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface IParallelTextCorpus : IEnumerable<ParallelTextRow>
	{
		bool MissingRowsAllowed { get; }

		int Count(bool includeEmpty = true);

		IEnumerable<ParallelTextRow> GetRows();
	}
}
