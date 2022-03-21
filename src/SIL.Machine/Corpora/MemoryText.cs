using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class MemoryText : IText
	{
		private readonly TextCorpusRow[] _rows;

		public MemoryText(string id)
			: this(id, Enumerable.Empty<TextCorpusRow>())
		{
		}

		public MemoryText(string id, IEnumerable<TextCorpusRow> rows)
		{
			Id = id;
			_rows = rows.ToArray();
		}

		public string Id { get; }
		public string SortKey => Id;

		public IEnumerable<TextCorpusRow> GetRows()
		{
			return _rows;
		}
	}
}
