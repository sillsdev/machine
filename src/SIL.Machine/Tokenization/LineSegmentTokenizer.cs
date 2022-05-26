using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
    public class LineSegmentTokenizer : StringTokenizer
    {
        public override IEnumerable<Range<int>> TokenizeAsRanges(string data, Range<int> range)
        {
            int lineStart = range.Start;
            for (int i = range.Start; i < range.End; i++)
            {
                if (data[i] == '\n' || data[i] == '\r')
                {
                    yield return Range<int>.Create(lineStart, i);
                    if (data[i] == '\r' && i + 1 < range.End && data[i + 1] == '\n')
                        i++;
                    lineStart = i + 1;
                }
            }

            if (lineStart < range.End)
                yield return Range<int>.Create(lineStart, range.End);
        }
    }
}
