using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.Machine.Annotations;

namespace SIL.Machine.Tokenization
{
	public class LatinSentenceTokenizer : LatinWordTokenizer
	{
		private static readonly HashSet<string> SentenceTerminals = new HashSet<string>
		{
			".", "!", "?", "\u203C", "\u203D", "\u2047", "\u2048", "\u2049", "\u3002", "\uFE52", "\uFE57", "\uFF01",
			"\uFF0E", "\uFF1F", "\uFF61"
		};
		private static readonly HashSet<string> ClosingQuotes = new HashSet<string>
		{
			"\'", "\u2019", "\"", "\u201D", "»", "›"
		};
		private static readonly HashSet<string> ClosingBrackets = new HashSet<string> { "]", ")" };
		private static readonly Regex NewLineRegex = new Regex("\n|\r\n?");

		public LatinSentenceTokenizer()
			: this(Enumerable.Empty<string>())
		{
		}

		public LatinSentenceTokenizer(IEnumerable<string> abbreviations)
			: base(abbreviations)
		{
		}

		public override IEnumerable<Span<int>> Tokenize(string data, Span<int> dataSpan)
		{
			int lineStart = 0;
			foreach (Match match in NewLineRegex.Matches(data.Substring(0, dataSpan.End), dataSpan.Start))
			{
				foreach (Span<int> sentenceSpan in TokenizeLine(data, lineStart, match.Index + match.Length))
					yield return sentenceSpan;
				lineStart = match.Index + match.Length;
			}

			if (lineStart < dataSpan.End)
			{
				foreach (Span<int> sentenceSpan in TokenizeLine(data, lineStart, dataSpan.End))
					yield return sentenceSpan;
			}
		}

		private IEnumerable<Span<int>> TokenizeLine(string data, int start, int end)
		{
			int sentenceStart = -1, sentenceEnd = -1;
			bool inEnd = false, hasEndQuotesBrackets = false;
			foreach (Span<int> wordSpan in base.Tokenize(data, Span<int>.Create(start, end)))
			{
				if (sentenceStart == -1)
					sentenceStart = wordSpan.Start;
				string word = data.Substring(wordSpan.Start, wordSpan.Length);
				if (!inEnd)
				{
					if (SentenceTerminals.Contains(word))
						inEnd = true;
				}
				else
				{
					if (ClosingQuotes.Contains(word) || ClosingBrackets.Contains(word))
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
						yield return Span<int>.Create(sentenceStart, sentenceEnd);
						sentenceStart = wordSpan.Start;
						inEnd = false;
						hasEndQuotesBrackets = false;
					}
				}
				sentenceEnd = wordSpan.End;
			}

			if (sentenceStart != -1 && sentenceEnd != -1)
				yield return Span<int>.Create(sentenceStart, inEnd ? sentenceEnd : end);
		}
	}
}
