using Newtonsoft.Json;

namespace SIL.Machine.WebApi
{
    public class DataFileDto : ResourceDto
    {
        [JsonProperty(Required = Required.DisallowNull)]
        public ResourceDto Engine { get; set; }
        public DataType DataType { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public string Name { get; set; }
        public FileFormat Format { get; set; }
        public CorpusType? CorpusType { get; set; }
        public string CorpusKey { get; set; }
    }
}
