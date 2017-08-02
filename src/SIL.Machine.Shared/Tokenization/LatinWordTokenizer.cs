using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public class LatinWordTokenizer : WhitespaceTokenizer
	{
		private static readonly Regex InnerWordPunctRegex = new Regex("\\G[&'\\-.:=?@\xAD\xB7\u2010\u2011\u2019\u2027]|_+");
		private readonly HashSet<string> _abbreviations;

		public LatinWordTokenizer()
			: this(new IntegerSpanFactory())
		{
		}

		public LatinWordTokenizer(IEnumerable<string> abbreviations)
			: this(new IntegerSpanFactory(), abbreviations)
		{
		}

		public LatinWordTokenizer(SpanFactory<int> spanFactory)
			: this(spanFactory, Enumerable.Empty<string>())
		{
		}

		public LatinWordTokenizer(SpanFactory<int> spanFactory, IEnumerable<string> abbreviations)
			: base(spanFactory)
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
					if (IsPunctuation(data[i]) || char.IsSymbol(data[i]) || char.IsControl(data[i]))
					{
						if (wordStart == -1)
						{
							yield return SpanFactory.Create(i);
						}
						else if (innerWordPunct != -1)
						{
							yield return SpanFactory.Create(wordStart, innerWordPunct);
							yield return SpanFactory.Create(innerWordPunct, i);
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

							yield return SpanFactory.Create(wordStart, i);
							yield return SpanFactory.Create(i);
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
							yield return SpanFactory.Create(wordStart, span.End);
						}
						else
						{
							yield return SpanFactory.Create(wordStart, innerWordPunct);
							yield return SpanFactory.Create(innerWordPunct, span.End);
						}
					}
					else
					{
						yield return SpanFactory.Create(wordStart, span.End);
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

		private bool IsPunctuation(char c)
		{
#if BRIDGE_NET
			if (c < 256)
				return char.IsPunctuation(c);
			// this is a horrible workaround for a bug in Bridge.NET, see issue #2981
			return Regex.IsMatch(c.ToString(),
				"[\u037E\u0387\u055A-\u055F\u0589\u058A\u05BE\u05C0\u05C3\u05C6\u05F3\u05F4\u0609\u060A\u060C\u060D\u061B\u061E\u061F\u066A-\u066D\u06D4\u0700-\u070D\u07F7-\u07F9\u0830-\u083E\u085E\u0964\u0965\u0970\u0AF0\u0DF4\u0E4F\u0E5A\u0E5B\u0F04-\u0F12\u0F14\u0F3A-\u0F3D\u0F85\u0FD0-\u0FD4\u0FD9\u0FDA\u104A-\u104F\u10FB\u1360-\u1368\u1400\u166D\u166E\u169B\u169C\u16EB-\u16ED\u1735\u1736\u17D4-\u17D6\u17D8-\u17DA\u1800-\u180A\u1944\u1945\u1A1E\u1A1F\u1AA0-\u1AA6\u1AA8-\u1AAD\u1B5A-\u1B60\u1BFC-\u1BFF\u1C3B-\u1C3F\u1C7E\u1C7F\u1CC0-\u1CC7\u1CD3\u2010-\u2027\u2030-\u2043\u2045-\u2051\u2053-\u205E\u207D\u207E\u208D\u208E\u2329\u232A\u2768-\u2775\u27C5\u27C6\u27E6-\u27EF\u2983-\u2998\u29D8-\u29DB\u29FC\u29FD\u2CF9-\u2CFC\u2CFE\u2CFF\u2D70\u2E00-\u2E2E\u2E30-\u2E3B\u3001-\u3003\u3008-\u3011\u3014-\u301F\u3030\u303D\u30A0\u30FB\uA4FE\uA4FF\uA60D-\uA60F\uA673\uA67E\uA6F2-\uA6F7\uA874-\uA877\uA8CE\uA8CF\uA8F8-\uA8FA\uA92E\uA92F\uA95F\uA9C1-\uA9CD\uA9DE\uA9DF\uAA5C-\uAA5F\uAADE\uAADF\uAAF0\uAAF1\uABEB\uFD3E\uFD3F\uFE10-\uFE19\uFE30-\uFE52\uFE54-\uFE61\uFE63\uFE68\uFE6A\uFE6B\uFF01-\uFF03\uFF05-\uFF0A\uFF0C-\uFF0F\uFF1A\uFF1B\uFF1F\uFF20\uFF3B-\uFF3D\uFF3F\uFF5B\uFF5D\uFF5F-\uFF65\u058A\u05BE\u1400\u1806\u2010-\u2015\u2E17\u2E1A\u2E3A\u2E3B\u301C\u3030\u30A0\uFE31\uFE32\uFE58\uFE63\uFF0D\u0F3A\u0F3C\u169B\u201A\u201E\u2045\u207D\u208D\u2329\u2768\u276A\u276C\u276E\u2770\u2772\u2774\u27C5\u27E6\u27E8\u27EA\u27EC\u27EE\u2983\u2985\u2987\u2989\u298B\u298D\u298F\u2991\u2993\u2995\u2997\u29D8\u29DA\u29FC\u2E22\u2E24\u2E26\u2E28\u3008\u300A\u300C\u300E\u3010\u3014\u3016\u3018\u301A\u301D\uFD3E\uFE17\uFE35\uFE37\uFE39\uFE3B\uFE3D\uFE3F\uFE41\uFE43\uFE47\uFE59\uFE5B\uFE5D\uFF08\uFF3B\uFF5B\uFF5F\uFF62\u0F3B\u0F3D\u169C\u2046\u207E\u208E\u232A\u2769\u276B\u276D\u276F\u2771\u2773\u2775\u27C6\u27E7\u27E9\u27EB\u27ED\u27EF\u2984\u2986\u2988\u298A\u298C\u298E\u2990\u2992\u2994\u2996\u2998\u29D9\u29DB\u29FD\u2E23\u2E25\u2E27\u2E29\u3009\u300B\u300D\u300F\u3011\u3015\u3017\u3019\u301B\u301E\u301F\uFD3F\uFE18\uFE36\uFE38\uFE3A\uFE3C\uFE3E\uFE40\uFE42\uFE44\uFE48\uFE5A\uFE5C\uFE5E\uFF09\uFF3D\uFF5D\uFF60\uFF63\u2018\u201B\u201C\u201F\u2039\u2E02\u2E04\u2E09\u2E0C\u2E1C\u2E20\u2019\u201D\u203A\u2E03\u2E05\u2E0A\u2E0D\u2E1D\u2E21\u203F\u2040\u2054\uFE33\uFE34\uFE4D-\uFE4F\uFF3F\u037E\u0387\u055A-\u055F\u0589\u05C0\u05C3\u05C6\u05F3\u05F4\u0609\u060A\u060C\u060D\u061B\u061E\u061F\u066A-\u066D\u06D4\u0700-\u070D\u07F7-\u07F9\u0830-\u083E\u085E\u0964\u0965\u0970\u0AF0\u0DF4\u0E4F\u0E5A\u0E5B\u0F04-\u0F12\u0F14\u0F85\u0FD0-\u0FD4\u0FD9\u0FDA\u104A-\u104F\u10FB\u1360-\u1368\u166D\u166E\u16EB-\u16ED\u1735\u1736\u17D4-\u17D6\u17D8-\u17DA\u1800-\u1805\u1807-\u180A\u1944\u1945\u1A1E\u1A1F\u1AA0-\u1AA6\u1AA8-\u1AAD\u1B5A-\u1B60\u1BFC-\u1BFF\u1C3B-\u1C3F\u1C7E\u1C7F\u1CC0-\u1CC7\u1CD3\u2016\u2017\u2020-\u2027\u2030-\u2038\u203B-\u203E\u2041-\u2043\u2047-\u2051\u2053\u2055-\u205E\u2CF9-\u2CFC\u2CFE\u2CFF\u2D70\u2E00\u2E01\u2E06-\u2E08\u2E0B\u2E0E-\u2E16\u2E18\u2E19\u2E1B\u2E1E\u2E1F\u2E2A-\u2E2E\u2E30-\u2E39\u3001-\u3003\u303D\u30FB\uA4FE\uA4FF\uA60D-\uA60F\uA673\uA67E\uA6F2-\uA6F7\uA874-\uA877\uA8CE\uA8CF\uA8F8-\uA8FA\uA92E\uA92F\uA95F\uA9C1-\uA9CD\uA9DE\uA9DF\uAA5C-\uAA5F\uAADE\uAADF\uAAF0\uAAF1\uABEB\uFE10-\uFE16\uFE19\uFE30\uFE45\uFE46\uFE49-\uFE4C\uFE50-\uFE52\uFE54-\uFE57\uFE5F-\uFE61\uFE68\uFE6A\uFE6B\uFF01-\uFF03\uFF05-\uFF07\uFF0A\uFF0C\uFF0E\uFF0F\uFF1A\uFF1B\uFF1F\uFF20\uFF3C\uFF61\uFF64\uFF65]");
#else
			return char.IsPunctuation(c);
#endif
		}
	}
}
