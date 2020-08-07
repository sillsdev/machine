using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public class ZwspWordTokenizer : LatinWordTokenizer
	{
		protected override (Range<int>, Range<int>) ProcessCharacter(string data, Range<int> range,
			TokenizeContext ctxt)
		{
			if (char.IsWhiteSpace(data[ctxt.Index]))
			{
				int endIndex = ctxt.Index + 1;
				while (endIndex != range.End && char.IsWhiteSpace(data[endIndex]))
					endIndex++;
				var tokenRanges = (Range<int>.Null, Range<int>.Null);
				// ignore whitespace that is followed by whitespace or punctuation
				if (ctxt.Index != range.End - 1
					&& (char.IsPunctuation(data[endIndex]) || char.IsWhiteSpace(data[endIndex])))
				{
					if (ctxt.WordStart != -1)
					{
						tokenRanges = (Range<int>.Create(ctxt.WordStart, ctxt.Index), Range<int>.Null);
						ctxt.WordStart = -1;
					}
				}
				// ignore whitespace that is preceded by whitespace or punctuation
				else if (ctxt.Index != range.Start
					&& (char.IsPunctuation(data[ctxt.Index - 1]) || char.IsWhiteSpace(data[ctxt.Index - 1])))
				{
					if (ctxt.InnerWordPunct != -1)
					{
						tokenRanges = (Range<int>.Create(ctxt.WordStart, ctxt.InnerWordPunct),
							Range<int>.Create(ctxt.InnerWordPunct));
						ctxt.WordStart = -1;
					}
				}
				else if (ctxt.WordStart == -1)
				{
					tokenRanges = (Range<int>.Create(ctxt.Index, endIndex), Range<int>.Null);
				}
				else if (ctxt.InnerWordPunct != -1)
				{
					tokenRanges = (Range<int>.Create(ctxt.WordStart, ctxt.InnerWordPunct),
						Range<int>.Create(ctxt.InnerWordPunct, ctxt.Index));
					ctxt.WordStart = ctxt.Index;
				}
				else
				{
					tokenRanges = (Range<int>.Create(ctxt.WordStart, ctxt.Index),
						Range<int>.Create(ctxt.Index, endIndex));
					ctxt.WordStart = -1;
				}
				ctxt.InnerWordPunct = -1;
				ctxt.Index = endIndex;
				return tokenRanges;
			}

			return base.ProcessCharacter(data, range, ctxt);
		}

		protected override bool IsWhitespace(char c)
		{
			return c == '\u200b' || c == '\ufeff';
		}
	}
}
