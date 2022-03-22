using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface IAlignmentCollection
	{
		string Id { get; }

		string SortKey { get; }

		IEnumerable<AlignmentRow> GetRows();
	}
}
