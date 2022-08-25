using System.Runtime.Serialization;

namespace SIL.Machine.WebApi
{
    [DataContract]
    public class AlignedWordPairDto
    {
        [DataMember(Order = 1)]
        public int SourceIndex { get; set; }

        [DataMember(Order = 2)]
        public int TargetIndex { get; set; }
    }
}
