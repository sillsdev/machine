using System.Collections.Generic;

namespace SIL.Machine.WebApi.Models
{
	public class TranslationResultDto
	{
		public IReadOnlyList<string> Target { get; set; }
		public IReadOnlyList<double> Confidences { get; set; }
		public IReadOnlyList<AlignedWordPairDto> Alignment { get; set; }
	}
}
