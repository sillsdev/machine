using System.Collections.Generic;

namespace SIL.Machine.WebApi.Models
{
	public class SegmentPairDto
	{
		public IReadOnlyList<string> SourceSegment { get; set; }
		public IReadOnlyList<string> TargetSegment { get; set; }
	}
}
