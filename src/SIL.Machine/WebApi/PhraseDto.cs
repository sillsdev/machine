using System.Runtime.Serialization;

namespace SIL.Machine.WebApi
{
    [DataContract]
    public class PhraseDto
    {
        [DataMember(Order = 1)]
        public RangeDto SourceSegmentRange { get; set; }

        [DataMember(Order = 2)]
        public int TargetSegmentCut { get; set; }

        [DataMember(Order = 3)]
        public double Confidence { get; set; }
    }
}
