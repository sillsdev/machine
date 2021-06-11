using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class NullText : IText
	{
		public NullText(string id, string sortKey)
		{
			Id = id;
			SortKey = sortKey;
		}

		public string Id { get; }

		public string SortKey { get; }

		public IEnumerable<TextSegment> GetSegments(bool includeText = true)
		{
			return Enumerable.Empty<TextSegment>();
		}
	}
}
