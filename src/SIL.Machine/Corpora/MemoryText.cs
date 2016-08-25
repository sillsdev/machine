using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class MemoryText : IText
	{
		private readonly TextSegment[] _segments;

		public MemoryText(string id, IEnumerable<TextSegment> segments)
		{
			Id = id;
			_segments = segments.ToArray();
		}

		public string Id { get; }

		public IEnumerable<TextSegment> Segments => _segments;
	}
}
