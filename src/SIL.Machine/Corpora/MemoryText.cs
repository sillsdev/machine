using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class MemoryText : IText
	{
		private readonly TextRow[] _rows;

		public MemoryText(string id)
			: this(id, Enumerable.Empty<TextRow>())
		{
		}

		public MemoryText(string id, IEnumerable<TextRow> rows)
		{
			Id = id;
			_rows = rows.ToArray();
		}

		public string Id { get; }
		public string SortKey => Id;

		public bool MissingRowsAllowed => true;

		public int Count(bool includeEmpty = true)
		{
			return includeEmpty ? _rows.Length : GetRows().Count(r => !r.IsEmpty);
		}

		public IEnumerable<TextRow> GetRows()
		{
			return _rows;
		}
	}
}
