using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class MemoryAlignmentCollection : IAlignmentCollection
	{
		private readonly AlignmentRow[] _rows;

		public MemoryAlignmentCollection(string id)
			: this(id, Enumerable.Empty<AlignmentRow>())
		{
		}

		public MemoryAlignmentCollection(string id, IEnumerable<AlignmentRow> rows)
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

		public IEnumerable<AlignmentRow> GetRows()
		{
			return _rows;
		}
	}
}
