using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
	public class MemoryText : IText
	{
		public MemoryText(string id, IEnumerable<TextSegment> segments)
		{
			Id = id;
			Segments = segments.ToArray();
		}

		public string Id { get; }
		public IEnumerable<TextSegment> Segments { get; }
	}
}
