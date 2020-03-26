using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface IText
	{
		string Id { get; }

		string SortKey { get; }

		IEnumerable<TextSegment> Segments { get; }
	}
}
