using System.Collections.Generic;
using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Models
{
	public class TranslationResultDto
	{
		public IReadOnlyList<string> Target { get; set; }
		public IReadOnlyList<float> Confidences { get; set; }
		public IReadOnlyList<TranslationSources> Sources { get; set; }
		public IReadOnlyList<AlignedWordPairDto> Alignment { get; set; }
	}
}
