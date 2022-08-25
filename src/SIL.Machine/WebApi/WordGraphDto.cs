using System.Runtime.Serialization;

namespace SIL.Machine.WebApi
{
    [DataContract]
    public class WordGraphDto
    {
        [DataMember(Order = 1)]
        public float InitialStateScore { get; set; }

        [DataMember(Order = 2)]
        public int[] FinalStates { get; set; }

        [DataMember(Order = 3)]
        public WordGraphArcDto[] Arcs { get; set; }
    }
}
