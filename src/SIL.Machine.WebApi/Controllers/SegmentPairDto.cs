using System.Collections.Generic;

namespace SIL.Machine.WebApi.Controllers
{
	public class SegmentPairDto
	{
		public IReadOnlyList<string> SourceSegment { get; set; }
		public IReadOnlyList<string> TargetSegment { get; set; }
	}
}
