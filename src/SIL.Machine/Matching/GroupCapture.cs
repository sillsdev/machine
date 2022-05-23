using SIL.Machine.Annotations;

namespace SIL.Machine.Matching
{
    public class GroupCapture<TOffset>
    {
        internal GroupCapture(string name, Range<TOffset> range)
        {
            Name = name;
            Range = range;
        }

        public string Name { get; }
        public Range<TOffset> Range { get; }
        public bool Success => Range != Range<TOffset>.Null;
    }
}
