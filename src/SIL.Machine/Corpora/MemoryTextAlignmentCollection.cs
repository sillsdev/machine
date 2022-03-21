using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class MemoryTextAlignmentCollection : ITextAlignmentCollection
	{
		private readonly TextAlignmentCorpusRow[] _rows;

		public MemoryTextAlignmentCollection(string id)
			: this(id, Enumerable.Empty<TextAlignmentCorpusRow>())
		{
		}

		public MemoryTextAlignmentCollection(string id, IEnumerable<TextAlignmentCorpusRow> rows)
		{
			Id = id;
			_rows = rows.ToArray();
		}

		public string Id { get; }

		public string SortKey => Id;

		public IEnumerable<TextAlignmentCorpusRow> GetRows()
		{
			return _rows;
		}
	}
}
