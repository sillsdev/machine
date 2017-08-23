using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Dtos
{
	public class TranslationResultDto
	{
		public string[] Target { get; set; }
		public float[] Confidences { get; set; }
		public TranslationSources[] Sources { get; set; }
		public AlignedWordPairDto[] Alignment { get; set; }
	}
}
