using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public abstract class CorpusBase<T> : ICorpus<T> where T : IRow
	{
		public virtual bool MissingRowsAllowed => true;

		public virtual int Count(bool includeEmpty = true)
		{
			return includeEmpty ? GetRows().Count() : GetRows().Count(r => !r.IsEmpty);
		}

		public abstract IEnumerable<T> GetRows();

		public IEnumerator<T> GetEnumerator()
		{
			return GetRows().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
