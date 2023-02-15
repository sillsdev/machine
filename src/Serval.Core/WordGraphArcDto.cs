namespace Serval.Core
{
    public class WordGraphArcDto
    {
        public int PrevState { get; set; }
        public int NextState { get; set; }
        public float Score { get; set; }
        public string[] Tokens { get; set; }
        public float[] Confidences { get; set; }
        public int SourceSegmentStart { get; set; }
        public int SourceSegmentEnd { get; set; }
        public AlignedWordPairDto[] Alignment { get; set; }
        public TranslationSources[] Sources { get; set; }
    }
}
