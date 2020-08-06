using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public class LatinWordTokenizer : WhitespaceTokenizer
	{
		private static readonly Regex InnerWordPunctRegex = new Regex(
			"\\G[&\\-.:=,?@\xAD\xB7\u2010\u2011\u2019\u2027]|['_]+");
		private readonly HashSet<string> _abbreviations;

		public LatinWordTokenizer()
			: this(Enumerable.Empty<string>())
		{
		}

		public LatinWordTokenizer(IEnumerable<string> abbreviations)
		{
			_abbreviations = new HashSet<string>(abbreviations.Select(a => a.ToLowerInvariant()));
		}

		public bool TreatApostropheAsSingleQuote = false;

		public override IEnumerable<Range<int>> Tokenize(string data, Range<int> range)
		{
			var ctxt = new TokenizeContext();
			foreach (Range<int> charRange in base.Tokenize(data, range))
			{
				ctxt.Index = charRange.Start;
				ctxt.WordStart = -1;
				ctxt.InnerWordPunct = -1;
				while (ctxt.Index < charRange.End)
				{
					(Range<int> tokenRange1, Range<int> tokenRange2) = ProcessCharacter(data, range, ctxt);
					if (tokenRange1 != Range<int>.Null)
						yield return tokenRange1;
					if (tokenRange2 != Range<int>.Null)
						yield return tokenRange2;
				}

				if (ctxt.WordStart != -1)
				{
					if (ctxt.InnerWordPunct != -1)
					{
						string innerPunctStr = data.Substring(ctxt.InnerWordPunct, charRange.End - ctxt.InnerWordPunct);
						if ((innerPunctStr == "." && IsAbbreviation(data, ctxt.WordStart, ctxt.InnerWordPunct))
							|| (innerPunctStr == "'" && !TreatApostropheAsSingleQuote))
						{
							yield return Range<int>.Create(ctxt.WordStart, charRange.End);
						}
						else
						{
							yield return Range<int>.Create(ctxt.WordStart, ctxt.InnerWordPunct);
							yield return Range<int>.Create(ctxt.InnerWordPunct, charRange.End);
						}
					}
					else
					{
						yield return Range<int>.Create(ctxt.WordStart, charRange.End);
					}
				}
			}
		}

		protected virtual (Range<int>, Range<int>) ProcessCharacter(string data, Range<int> range, TokenizeContext ctxt)
		{
			var tokenRanges = (Range<int>.Null, Range<int>.Null);
			char c = data[ctxt.Index];
			int endIndex = ctxt.Index + 1;
			if (char.IsPunctuation(c) || char.IsSymbol(c) || char.IsControl(c))
			{
				while (endIndex != range.End && data[endIndex] == c)
					endIndex++;
				if (ctxt.WordStart == -1)
				{
					if (c == '\'' && !TreatApostropheAsSingleQuote)
						ctxt.WordStart = ctxt.Index;
					else
						tokenRanges = (Range<int>.Create(ctxt.Index, endIndex), Range<int>.Null);
				}
				else if (ctxt.InnerWordPunct != -1)
				{
					string innerPunctStr = data.Substring(ctxt.InnerWordPunct, ctxt.Index - ctxt.InnerWordPunct);
					if (innerPunctStr == "'" && !TreatApostropheAsSingleQuote)
					{
						tokenRanges = (Range<int>.Create(ctxt.WordStart, ctxt.Index), Range<int>.Null);
					}
					else
					{
						tokenRanges = (Range<int>.Create(ctxt.WordStart, ctxt.InnerWordPunct),
							Range<int>.Create(ctxt.InnerWordPunct, ctxt.Index));
					}
					ctxt.WordStart = ctxt.Index;
				}
				else
				{
					Match match = InnerWordPunctRegex.Match(data, ctxt.Index);
					if (match.Success)
					{
						ctxt.InnerWordPunct = ctxt.Index;
						ctxt.Index += match.Length;
						return (Range<int>.Null, Range<int>.Null);
					}

					tokenRanges = (Range<int>.Create(ctxt.WordStart, ctxt.Index),
						Range<int>.Create(ctxt.Index, endIndex));
					ctxt.WordStart = -1;
				}
			}
			else if (ctxt.WordStart == -1)
			{
				ctxt.WordStart = ctxt.Index;
			}

			ctxt.InnerWordPunct = -1;
			ctxt.Index = endIndex;
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
