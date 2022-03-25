using System;
using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public class TextRow
	{
		public TextRow(object rowRef)
		{
			Ref = rowRef;
		}

		public object Ref { get; }

		public bool IsEmpty { get; set; } = true;

		public bool IsSentenceStart { get; set; } = true;

		public bool IsInRange { get; set; }
		public bool IsRangeStart { get; set; }


		public IReadOnlyList<string> Segment { get; set; } = Array.Empty<string>();
		public string Text => string.Join(" ", Segment);

		public override string ToString()
		{
			string segment;
			if (IsEmpty)
				segment = IsInRange ? "<range>" : "EMPTY";
			else if (Segment.Count > 0)
				segment = Text;
			else
				segment = "NONEMPTY";
			return $"{Ref} - {segment}";
		}
	}
}
