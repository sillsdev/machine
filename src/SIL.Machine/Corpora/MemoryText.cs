using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class MemoryText : IText
	{
		private readonly TextSegment[] _segments;

		public MemoryText(string id)
			: this(id, Enumerable.Empty<TextSegment>())
		{
		}

		public MemoryText(string id, IEnumerable<TextSegment> segments)
		{
			Id = id;
			_segments = segments.ToArray();
		}

		public string Id { get; }
		public string SortKey => Id;

		public IEnumerable<TextSegment> GetSegments(bool includeText = true, IText sortBasedOn = null)
		{
			return _segments;
		}
	}
}
