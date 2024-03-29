﻿using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
    public class NullTokenizer : StringTokenizer
    {
        public override IEnumerable<Range<int>> TokenizeAsRanges(string data, Range<int> range)
        {
            if (range.Length > 0)
                yield return range;
        }
    }
}
