using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
    public static class TokenizationExtensions
    {
        public static string[] Split(this string str, IEnumerable<Range<int>> ranges)
        {
            return ranges.Select(r => str.Substring(r.Start, r.Length)).ToArray();
        }

        public static IEnumerable<Range<int>> GetRanges(this string str, IEnumerable<string> tokens)
        {
            int start = 0;
            foreach (string token in tokens)
            {
                int index = str.IndexOf(token, start);
                if (index == -1)
                    throw new ArgumentException(
                        $"The string does not contain the specified token: {token}.",
                        nameof(tokens)
                    );
                yield return Range<int>.Create(index, index + token.Length);
                start = index + token.Length;
            }
        }
    }
}
