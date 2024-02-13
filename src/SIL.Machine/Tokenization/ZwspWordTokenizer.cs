using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
    public class ZwspWordTokenizer : LatinWordTokenizer
    {
        protected override (Range<int>, Range<int>) ProcessCharacter(
            string data,
            Range<int> range,
            TokenizeContext context
        )
        {
            if (char.IsWhiteSpace(data[context.Index]))
            {
                int endIndex = context.Index + 1;
                while (endIndex != range.End && char.IsWhiteSpace(data[endIndex]))
                    endIndex++;
                (Range<int>, Range<int>) tokenRanges = (Range<int>.Null, Range<int>.Null);
                // ignore whitespace that is followed by whitespace or punctuation
                if (
                    context.Index != range.End - 1
                    && (char.IsPunctuation(data[endIndex]) || char.IsWhiteSpace(data[endIndex]))
                )
                {
                    if (context.WordStart != -1)
                    {
                        tokenRanges = (Range<int>.Create(context.WordStart, context.Index), Range<int>.Null);
                        context.WordStart = -1;
                    }
                }
                // ignore whitespace that is preceded by whitespace or punctuation
                else if (
                    context.Index != range.Start
                    && (char.IsPunctuation(data[context.Index - 1]) || char.IsWhiteSpace(data[context.Index - 1]))
                )
                {
                    if (context.InnerWordPunct != -1)
                    {
                        tokenRanges = (
                            Range<int>.Create(context.WordStart, context.InnerWordPunct),
                            Range<int>.Create(context.InnerWordPunct)
                        );
                        context.WordStart = -1;
                    }
                }
                else if (context.WordStart == -1)
                    tokenRanges = (Range<int>.Create(context.Index, endIndex), Range<int>.Null);
                else if (context.InnerWordPunct != -1)
                {
                    tokenRanges = (
                        Range<int>.Create(context.WordStart, context.InnerWordPunct),
                        Range<int>.Create(context.InnerWordPunct, context.Index)
                    );
                    context.WordStart = context.Index;
                }
                else
                {
                    tokenRanges = (
                        Range<int>.Create(context.WordStart, context.Index),
                        Range<int>.Create(context.Index, endIndex)
                    );
                    context.WordStart = -1;
                }
                context.InnerWordPunct = -1;
                context.Index = endIndex;
                return tokenRanges;
            }

            return base.ProcessCharacter(data, range, context);
        }

        protected override bool IsWhitespace(char c)
        {
            return c == '\u200b' || c == '\ufeff';
        }
    }
}
