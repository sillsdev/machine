using System;
using Newtonsoft.Json;

namespace Serval.Core
{
    public class BuildDto : ResourceDto
    {
        public int Revision { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public ResourceDto Parent { get; set; }
        public int Step { get; set; }
        public double? PercentCompleted { get; set; }
        public string Message { get; set; }

        /// <summary>
        /// The current build job state.
        /// </summary>
        [JsonProperty(Required = Required.DisallowNull)]
        public BuildState State { get; set; }
        public DateTime? DateFinished { get; set; }
    }
}
