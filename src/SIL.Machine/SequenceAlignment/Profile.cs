using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.SequenceAlignment
{
    public class Profile<TSeq, TItem>
    {
        private readonly Alignment<TSeq, TItem> _alignment;
        private readonly double[] _weights;

        public Profile(Alignment<TSeq, TItem> alignment, IEnumerable<double> weights)
        {
            _alignment = alignment;
            _weights = weights.ToArray();
        }

        public Alignment<TSeq, TItem> Alignment
        {
            get { return _alignment; }
        }

        public double[] Weights
        {
            get { return _weights; }
        }
    }
}
