using System.Runtime.Serialization;

namespace SIL.Machine.WebApi
{
    [DataContract]
    public class RangeDto
    {
        [DataMember(Order = 1)]
        public int Start { get; set; }

        [DataMember(Order = 2)]
        public int End { get; set; }
    }
}
