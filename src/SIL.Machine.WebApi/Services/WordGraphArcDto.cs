using System.Collections.Generic;

namespace SIL.Machine.WebApi.Services
{
	public class WordGraphArcDto
	{
		public int PrevState { get; set; }
		public int NextState { get; set; }
		public float Score { get; set; }
		public IReadOnlyList<string> Words { get; set; }
		public IReadOnlyList<float> Confidences { get; set; }
		public int SourceStartIndex { get; set; }
		public int SourceEndIndex { get; set; }
		public bool IsUnknown { get; set; }
		public IReadOnlyList<AlignedWordPairDto> Alignment { get; set; }
	}
}
