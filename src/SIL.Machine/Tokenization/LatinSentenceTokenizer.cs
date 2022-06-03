using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Utils;

namespace SIL.Machine.Tokenization
{
    public class LatinSentenceTokenizer : LatinWordTokenizer
    {
        private static readonly LineSegmentTokenizer LineTokenizer = new LineSegmentTokenizer();

        public LatinSentenceTokenizer() : this(Enumerable.Empty<string>()) { }

        public LatinSentenceTokenizer(IEnumerable<string> abbreviations) : base(abbreviations) { }

        public override IEnumerable<Range<int>> TokenizeAsRanges(string data, Range<int> range)
        {
            foreach (Range<int> lineRange in LineTokenizer.TokenizeAsRanges(data, range))
            {
                foreach (Range<int> sentenceRange in TokenizeLine(data, lineRange))
                    yield return sentenceRange;
            }
        }

        private IEnumerable<Range<int>> TokenizeLine(string data, Range<int> lineRange)
        {
            int sentenceStart = -1,
                sentenceEnd = -1;
            bool inEnd = false,
                hasEndQuotesBrackets = false;
            foreach (Range<int> wordRange in base.TokenizeAsRanges(data, lineRange))
            {
                if (sentenceStart == -1)
                    sentenceStart = wordRange.Start;
                string word = data.Substring(wordRange.Start, wordRange.Length);
                if (!inEnd)
                {
                    if (word.IsSentenceTerminal())
                        inEnd = true;
                }
                else
                {
                    if (word.IsDelayedSentenceEnd())
                    {
                        hasEndQuotesBrackets = true;
                    }
                    else if (hasEndQuotesBrackets && char.IsLower(word[0]))
                    {
                        inEnd = false;
                        hasEndQuotesBrackets = false;
                    }
                    else
                    {
                        yield return Range<int>.Create(sentenceStart, sentenceEnd);
                        sentenceStart = wordRange.Start;
                        inEnd = false;
                        hasEndQuotesBrackets = false;
                    }
                }
                sentenceEnd = wordRange.End;
            }

            if (sentenceStart != -1 && sentenceEnd != -1)
                yield return Range<int>.Create(sentenceStart, inEnd ? sentenceEnd : lineRange.End);
        }
    }
}
