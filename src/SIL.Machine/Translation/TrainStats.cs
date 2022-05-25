using System.Collections.Generic;

namespace SIL.Machine.Translation
{
    public class TrainStats
    {
        public int TrainSize { get; set; }
        public IDictionary<string, double> Metrics { get; } = new Dictionary<string, double>();
    }
}
