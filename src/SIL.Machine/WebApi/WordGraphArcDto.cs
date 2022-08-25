using System.Runtime.Serialization;
using SIL.Machine.Translation;

namespace SIL.Machine.WebApi
{
    [DataContract]
    public class WordGraphArcDto
    {
        [DataMember(Order = 1)]
        public int PrevState { get; set; }

        [DataMember(Order = 2)]
        public int NextState { get; set; }

        [DataMember(Order = 3)]
        public float Score { get; set; }

        [DataMember(Order = 4)]
        public string[] Words { get; set; }

        [DataMember(Order = 5)]
        public float[] Confidences { get; set; }

        [DataMember(Order = 6)]
        public RangeDto SourceSegmentRange { get; set; }

        [DataMember(Order = 7)]
        public AlignedWordPairDto[] Alignment { get; set; }

        [DataMember(Order = 8)]
        public TranslationSources[] Sources { get; set; }
    }
}
