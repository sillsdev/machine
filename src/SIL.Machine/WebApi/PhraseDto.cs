namespace SIL.Machine.WebApi
{
    public class PhraseDto
    {
        public RangeDto SourceSegmentRange { get; set; }
        public int TargetSegmentCut { get; set; }
        public double Confidence { get; set; }
    }
}
