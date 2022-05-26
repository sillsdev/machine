using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
    public abstract class StringTokenizer : IRangeTokenizer<string, int, string>
    {
        public IEnumerable<string> Tokenize(string data)
        {
            return Tokenize(data, Range<int>.Create(0, data.Length));
        }

        public IEnumerable<string> Tokenize(string data, Range<int> range)
        {
            return TokenizeAsRanges(data, range).Select(r => data.Substring(r.Start, r.Length));
        }

        public IEnumerable<Range<int>> TokenizeAsRanges(string data)
        {
            return TokenizeAsRanges(data, Range<int>.Create(0, data.Length));
        }

        public abstract IEnumerable<Range<int>> TokenizeAsRanges(string data, Range<int> range);
    }
}
