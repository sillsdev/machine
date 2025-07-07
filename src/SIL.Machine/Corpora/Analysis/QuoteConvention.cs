using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.Machine.Corpora.Analysis
{
    public class SingleLevelQuoteConvention
    {
        public static readonly IReadOnlyDictionary<string, char> QuoteNormalizationMap = new Dictionary<string, char>()
        {
            { "\u00ab", '\'' },
            { "\u00bb", '"' },
            { "\u2018", '\'' },
            { "\u2019", '\'' },
            { "\u201a", '\'' },
            { "\u201c", '"' },
            { "\u201d", '"' },
            { "\u201e", '"' },
            { "\u300a", '"' },
            { "\u300b", '"' },
            { "\u300c", '"' },
            { "\u300d", '"' }
        };
        public string OpeningQuote { get; }
        public string ClosingQuote { get; }

        public SingleLevelQuoteConvention(string openingQuote, string closingQuote)
        {
            OpeningQuote = openingQuote;
            ClosingQuote = closingQuote;
        }

        public SingleLevelQuoteConvention Normalize()
        {
            string normalizedOpeningQuote = QuoteNormalizationMap.TryGetValue(OpeningQuote, out char quote)
                ? quote.ToString()
                : OpeningQuote;
            string normalizedClosingQuote = QuoteNormalizationMap.TryGetValue(ClosingQuote, out quote)
                ? quote.ToString()
                : ClosingQuote;
            return new SingleLevelQuoteConvention(normalizedOpeningQuote, normalizedClosingQuote);
        }
    }

    public class QuoteConvention
    {
        public string Name { get; }

        public IReadOnlyList<SingleLevelQuoteConvention> Levels { get; }

        public QuoteConvention(string name, List<SingleLevelQuoteConvention> levels)
        {
            Name = name;
            Levels = levels;
        }

        public int NumLevels => Levels.Count;

        public string GetOpeningQuoteAtLevel(int level)
        {
            return Levels[level - 1].OpeningQuote;
        }

        public string GetClosingQuoteAtLevel(int level)
        {
            return Levels[level - 1].ClosingQuote;
        }

        public string GetExpectedQuotationMark(int depth, QuotationMarkDirection direction)
        {
            if (depth > NumLevels || depth < 1)
                return "";
            return direction == QuotationMarkDirection.Opening
                ? GetOpeningQuoteAtLevel(depth)
                : GetClosingQuoteAtLevel(depth);
        }

        private bool IncludesOpeningQuotationMark(string openingQuotationMark)
        {
            foreach (SingleLevelQuoteConvention level in Levels)
            {
                if (level.OpeningQuote == openingQuotationMark)
                    return true;
            }
            return false;
        }

        private bool IncludesClosingQuotationMark(string closingQuotationMark)
        {
            foreach (SingleLevelQuoteConvention level in Levels)
            {
                if (level.ClosingQuote == closingQuotationMark)
                    return true;
            }
            return false;
        }

        public HashSet<int> GetPossibleDepths(string quotationMark, QuotationMarkDirection direction)
        {
            var depths = new HashSet<int>();
            foreach ((int depth, SingleLevelQuoteConvention level) in Levels.Select((l, i) => (i + 1, l)))
            {
                if (direction == QuotationMarkDirection.Opening && level.OpeningQuote == quotationMark)
                    depths.Add(depth);
                else if (direction == QuotationMarkDirection.Closing && level.ClosingQuote == quotationMark)
                    depths.Add(depth);
            }
            return depths;
        }

        public bool IsCompatibleWithObservedQuotationMarks(
            List<string> openingQuotationMarks,
            List<string> closingQuotationMarks
        )
        {
            foreach (string openingQuotationMark in openingQuotationMarks)
            {
                if (!IncludesOpeningQuotationMark(openingQuotationMark))
                    return false;
            }
            foreach (string closingQuotationMark in closingQuotationMarks)
            {
                if (!IncludesClosingQuotationMark(closingQuotationMark))
                    return false;
            }

            // we require the first-level quotes to have been observed
            if (!openingQuotationMarks.Contains(GetOpeningQuoteAtLevel(1)))
                return false;
            if (!closingQuotationMarks.Contains(GetClosingQuoteAtLevel(1)))
                return false;
            return true;
        }

        public QuoteConvention Normalize()
        {
            return new QuoteConvention(Name + "_normalized", Levels.Select(l => l.Normalize()).ToList());
        }

        public void PrintSummary()
        {
            Console.WriteLine(GetSummaryMessage());
        }

        private string GetSummaryMessage()
        {
            var summary = new StringBuilder(Name + "\n");
            foreach ((int level, SingleLevelQuoteConvention convention) in Levels.Select((l, i) => (i, l)))
            {
                string ordinalName = GetOrdinalName(level + 1);
                summary.Append($"{convention.OpeningQuote}{ordinalName}-level quote{convention.ClosingQuote}\n");
            }
            return summary.ToString();
        }

        private string GetOrdinalName(int level)
        {
            switch (level)
            {
                case 1:
                    return "First";
                case 2:
                    return "Second";
                case 3:
                    return "Third";
                case 4:
                    return "Fourth";
                default:
                    return level.ToString() + "th";
            }
        }
    }
}
