using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
    public class LatinWordTokenizer : WhitespaceTokenizer
    {
        private static readonly Regex s_innerWordPunctuationRegex = new Regex(
            @"\G[&\-.:=,?@\xAD\xB7\u2010\u2011\u2019\u2027]|['_]+"
        );
        private static readonly Regex s_urlRegex = new Regex(
            @"^(?:[\w-]+://?|www[.])[^\s()<>]+(?:[\w\d]+|(?:[^\p{P}\s]|/))",
            RegexOptions.IgnoreCase
        );
        private readonly HashSet<string> _abbreviations;

        public LatinWordTokenizer()
            : this(Enumerable.Empty<string>()) { }

        public LatinWordTokenizer(IEnumerable<string> abbreviations)
        {
            _abbreviations = new HashSet<string>(abbreviations.Select(a => a.ToLowerInvariant()));
        }

        public bool _treatApostropheAsSingleQuote = false;

        public override IEnumerable<Range<int>> TokenizeAsRanges(string data, Range<int> range)
        {
            var context = new TokenizeContext();
            foreach (Range<int> charRange in base.TokenizeAsRanges(data, range))
            {
                Match urlMatch = s_urlRegex.Match(data.Substring(charRange.Start, charRange.Length));
                if (urlMatch.Success)
                {
                    yield return Range<int>.Create(charRange.Start, charRange.Start + urlMatch.Length);
                    context.Index = charRange.Start + urlMatch.Length;
                }
                else
                    context.Index = charRange.Start;

                context.WordStart = -1;
                context.InnerWordPunct = -1;
                while (context.Index < charRange.End)
                {
                    (Range<int> tokenRange1, Range<int> tokenRange2) = ProcessCharacter(data, range, context);
                    if (tokenRange1 != Range<int>.Null)
                        yield return tokenRange1;
                    if (tokenRange2 != Range<int>.Null)
                        yield return tokenRange2;
                }

                if (context.WordStart != -1)
                {
                    if (context.InnerWordPunct != -1)
                    {
                        string innerPunctStr = data.Substring(
                            context.InnerWordPunct,
                            charRange.End - context.InnerWordPunct
                        );
                        if (
                            (innerPunctStr == "." && IsAbbreviation(data, context.WordStart, context.InnerWordPunct))
                            || (innerPunctStr == "'" && !_treatApostropheAsSingleQuote)
                        )
                        {
                            yield return Range<int>.Create(context.WordStart, charRange.End);
                        }
                        else
                        {
                            yield return Range<int>.Create(context.WordStart, context.InnerWordPunct);
                            yield return Range<int>.Create(context.InnerWordPunct, charRange.End);
                        }
                    }
                    else
                        yield return Range<int>.Create(context.WordStart, charRange.End);
                }
            }
        }

        protected virtual (Range<int>, Range<int>) ProcessCharacter(
            string data,
            Range<int> range,
            TokenizeContext context
        )
        {
            (Range<int>, Range<int>) tokenRanges = (Range<int>.Null, Range<int>.Null);
            char c = data[context.Index];
            int endIndex = context.Index + 1;
            if (char.IsPunctuation(c) || char.IsSymbol(c) || char.IsControl(c))
            {
                while (endIndex != range.End && data[endIndex] == c)
                    endIndex++;
                if (context.WordStart == -1)
                {
                    if (c == '\'' && !_treatApostropheAsSingleQuote)
                        context.WordStart = context.Index;
                    else
                        tokenRanges = (Range<int>.Create(context.Index, endIndex), Range<int>.Null);
                }
                else if (context.InnerWordPunct != -1)
                {
                    string innerPunctStr = data.Substring(
                        context.InnerWordPunct,
                        context.Index - context.InnerWordPunct
                    );
                    if (innerPunctStr == "'" && !_treatApostropheAsSingleQuote)
                        tokenRanges = (Range<int>.Create(context.WordStart, context.Index), Range<int>.Null);
                    else
                        tokenRanges = (
                            Range<int>.Create(context.WordStart, context.InnerWordPunct),
                            Range<int>.Create(context.InnerWordPunct, context.Index)
                        );
                    context.WordStart = context.Index;
                }
                else
                {
                    Match match = s_innerWordPunctuationRegex.Match(data, context.Index);
                    if (match.Success)
                    {
                        context.InnerWordPunct = context.Index;
                        context.Index += match.Length;
                        return tokenRanges;
                    }

                    tokenRanges = (
                        Range<int>.Create(context.WordStart, context.Index),
                        Range<int>.Create(context.Index, endIndex)
                    );
                    context.WordStart = -1;
                }
            }
            else if (context.WordStart == -1)
                context.WordStart = context.Index;

            context.InnerWordPunct = -1;
            context.Index = endIndex;
            return tokenRanges;
        }

        private bool IsAbbreviation(string data, int start, int end)
        {
            return _abbreviations.Contains(data.Substring(start, end - start).ToLowerInvariant());
        }

        protected class TokenizeContext
        {
            public int Index { get; set; }
            public int WordStart { get; set; }
            public int InnerWordPunct { get; set; }
        }
    }
}
