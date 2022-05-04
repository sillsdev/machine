using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ICorpus<T> : IEnumerable<T> where T : IRow
	{
		bool MissingRowsAllowed { get; }

		int Count(bool includeEmpty = true);

		IEnumerable<T> GetRows();
	}
}
