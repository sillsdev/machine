using System.Collections.Generic;

namespace SIL.Machine.WebApi.Models
{
	public class WordGraphArcDto
	{
		public int PrevState { get; set; }
		public int NextState { get; set; }
		public double Score { get; set; }
		public IReadOnlyList<string> Words { get; set; }
		public IReadOnlyList<double> Confidences { get; set; }
		public int SourceStartIndex { get; set; }
		public int SourceEndIndex { get; set; }
		public bool IsUnknown { get; set; }
		public IReadOnlyList<AlignedWordPairDto> Alignment { get; set; }
	}
}
