using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface IText
	{
		string Id { get; }

		string SortKey { get; }

		IEnumerable<TextSegment> GetSegments(bool includeText = true);

		IEnumerable<TextSegment> GetSegmentsBasedOn(IText text, bool includeText = true);
	}
}
