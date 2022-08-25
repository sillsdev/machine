using System.Runtime.Serialization;
using SIL.Machine.Translation;

namespace SIL.Machine.WebApi
{
    [DataContract]
    public class TranslationResultDto
    {
        [DataMember(Order = 1)]
        public string[] Target { get; set; }

        [DataMember(Order = 2)]
        public float[] Confidences { get; set; }

        [DataMember(Order = 3)]
        public TranslationSources[] Sources { get; set; }

        [DataMember(Order = 4)]
        public AlignedWordPairDto[] Alignment { get; set; }

        [DataMember(Order = 5)]
        public PhraseDto[] Phrases { get; set; }
    }
}
