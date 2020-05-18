using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public class LatinWordTokenizer : WhitespaceTokenizer
	{
		private static readonly Regex InnerWordPunctRegex = new Regex(
			"\\G[&\\-.:=?@\xAD\xB7\u2010\u2011\u2019\u2027]|['_]+");
		private readonly HashSet<string> _abbreviations;

		public LatinWordTokenizer()
			: this(Enumerable.Empty<string>())
		{
		}

		public LatinWordTokenizer(IEnumerable<string> abbreviations)
		{
			_abbreviations = new HashSet<string>(abbreviations.Select(ToLower));
		}

		public bool TreatApostropheAsSingleQuote = false;

		public override IEnumerable<Range<int>> Tokenize(string data, Range<int> range)
		{
			foreach (Range<int> charRange in base.Tokenize(data, range))
			{
				int wordStart = -1;
				int innerWordPunct = -1;
				int i = charRange.Start;
				while (i < charRange.End)
				{
					if (char.IsPunctuation(data[i]) || char.IsSymbol(data[i]) || char.IsControl(data[i]))
					{
						if (wordStart == -1)
						{
							if (data[i] == '\'' && !TreatApostropheAsSingleQuote)
								wordStart = i;
							else
								yield return Range<int>.Create(i);
						}
						else if (innerWordPunct != -1)
						{
							string innerPunctStr = data.Substring(innerWordPunct, i - innerWordPunct);
							if (innerPunctStr == "'" && !TreatApostropheAsSingleQuote)
							{
								yield return Range<int>.Create(wordStart, i);
							}
							else
							{
								yield return Range<int>.Create(wordStart, innerWordPunct);
								yield return Range<int>.Create(innerWordPunct, i);
							}
							wordStart = i;
						}
						else
						{
							Match match = InnerWordPunctRegex.Match(data, i);
							if (match.Success)
							{
								innerWordPunct = i;
								i += match.Length;
								continue;
							}

							yield return Range<int>.Create(wordStart, i);
							yield return Range<int>.Create(i);
							wordStart = -1;
						}
					}
					else if (wordStart == -1)
					{
						wordStart = i;
					}

					innerWordPunct = -1;
					i++;
				}

				if (wordStart != -1)
				{
					if (innerWordPunct != -1)
					{
						string innerPunctStr = data.Substring(innerWordPunct, charRange.End - innerWordPunct);
						if ((innerPunctStr == "." && IsAbbreviation(data, wordStart, innerWordPunct))
							|| (innerPunctStr == "'" && !TreatApostropheAsSingleQuote))
						{
							yield return Range<int>.Create(wordStart, charRange.End);
						}
						else
						{
							yield return Range<int>.Create(wordStart, innerWordPunct);
							yield return Range<int>.Create(innerWordPunct, charRange.End);
						}
					}
					else
					{
						yield return Range<int>.Create(wordStart, charRange.End);
					}
				}
			}
		}

		private string ToLower(string str)
		{
			return str.ToLowerInvariant();
		}

		private bool IsAbbreviation(string data, int start, int end)
		{
			return _abbreviations.Contains(ToLower(data.Substring(start, end - start)));
		}
	}
}
