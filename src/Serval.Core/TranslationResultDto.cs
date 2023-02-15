namespace Serval.Core
{
    public class TranslationResultDto
    {
        public string Translation { get; set; }

        public string[] Tokens { get; set; }

        public float[] Confidences { get; set; }

        public TranslationSources[] Sources { get; set; }

        public AlignedWordPairDto[] Alignment { get; set; }

        public PhraseDto[] Phrases { get; set; }
    }
}
