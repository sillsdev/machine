using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public class LatinWordTokenizer : WhitespaceTokenizer
	{
		private static readonly Regex InnerWordPunctRegex = new Regex(
			"\\G[&'\\-.:=?@\xAD\xB7\u2010\u2011\u2019\u2027]|_+");
		private readonly HashSet<string> _abbreviations;

		public LatinWordTokenizer()
			: this(Enumerable.Empty<string>())
		{
		}

		public LatinWordTokenizer(IEnumerable<string> abbreviations)
		{
			_abbreviations = new HashSet<string>(abbreviations.Select(ToLower));
		}

		public override IEnumerable<Span<int>> Tokenize(string data, Span<int> dataSpan)
		{
			foreach (Span<int> span in base.Tokenize(data, dataSpan))
			{
				int wordStart = -1;
				int innerWordPunct = -1;
				int i = span.Start;
				while (i < span.End)
				{
					if (char.IsPunctuation(data[i]) || char.IsSymbol(data[i]) || char.IsControl(data[i]))
					{
						if (wordStart == -1)
						{
							yield return Span<int>.Create(i);
						}
						else if (innerWordPunct != -1)
						{
							yield return Span<int>.Create(wordStart, innerWordPunct);
							yield return Span<int>.Create(innerWordPunct, i);
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

							yield return Span<int>.Create(wordStart, i);
							yield return Span<int>.Create(i);
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
						if (data.Substring(innerWordPunct, span.End - innerWordPunct) == "."
							&& _abbreviations.Contains(ToLower(data.Substring(wordStart, innerWordPunct - wordStart))))
						{
							yield return Span<int>.Create(wordStart, span.End);
						}
						else
						{
							yield return Span<int>.Create(wordStart, innerWordPunct);
							yield return Span<int>.Create(innerWordPunct, span.End);
						}
					}
					else
					{
						yield return Span<int>.Create(wordStart, span.End);
					}
				}
			}
		}

		private string ToLower(string str)
		{
#if BRIDGE_NET
			return str.ToLower();
#else
			return str.ToLowerInvariant();
#endif
		}
	}
}
