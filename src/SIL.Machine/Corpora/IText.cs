using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface IText
	{
		string Id { get; }

		IEnumerable<TextSegment> Segments { get; }
	}
}
