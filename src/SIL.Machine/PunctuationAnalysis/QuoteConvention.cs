using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.PunctuationAnalysis
{
    public class SingleLevelQuoteConvention
    {
        public static readonly IReadOnlyDictionary<string, char> QuoteNormalizationMap = new Dictionary<string, char>()
        {
            { "\u00ab", '\"' },
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
        public string OpeningQuotationMark { get; }
        public string ClosingQuotationMark { get; }

        public SingleLevelQuoteConvention(string openingQuotationMark, string closingQuotationMark)
        {
            OpeningQuotationMark = openingQuotationMark;
            ClosingQuotationMark = closingQuotationMark;
        }

        public SingleLevelQuoteConvention Normalize()
        {
            string normalizedOpeningQuotationMark = QuoteNormalizationMap.TryGetValue(
                OpeningQuotationMark,
                out char quote
            )
                ? quote.ToString()
                : OpeningQuotationMark;
            string normalizedClosingQuotationMark = QuoteNormalizationMap.TryGetValue(ClosingQuotationMark, out quote)
                ? quote.ToString()
                : ClosingQuotationMark;
            return new SingleLevelQuoteConvention(normalizedOpeningQuotationMark, normalizedClosingQuotationMark);
        }
    }

    public class QuoteConvention
    {
        public string Name { get; }

        public IReadOnlyList<SingleLevelQuoteConvention> LevelConventions { get; }

        public QuoteConvention(string name, List<SingleLevelQuoteConvention> levels)
        {
            Name = name;
            LevelConventions = levels;
        }

        public int NumLevels => LevelConventions.Count;

        public string GetOpeningQuotationMarkAtDepth(int depth)
        {
            return LevelConventions[depth - 1].OpeningQuotationMark;
        }

        public string GetClosingQuotationMarkAtDepth(int depth)
        {
            return LevelConventions[depth - 1].ClosingQuotationMark;
        }

        public string GetExpectedQuotationMark(int depth, QuotationMarkDirection direction)
        {
            if (depth > NumLevels || depth < 1)
                return "";
            return direction == QuotationMarkDirection.Opening
                ? GetOpeningQuotationMarkAtDepth(depth)
                : GetClosingQuotationMarkAtDepth(depth);
        }

        public bool IncludesOpeningQuotationMark(string openingQuotationMark)
        {
            foreach (SingleLevelQuoteConvention level in LevelConventions)
            {
                if (level.OpeningQuotationMark == openingQuotationMark)
                    return true;
            }
            return false;
        }

        public bool IncludesClosingQuotationMark(string closingQuotationMark)
        {
            foreach (SingleLevelQuoteConvention level in LevelConventions)
            {
                if (level.ClosingQuotationMark == closingQuotationMark)
                    return true;
            }
            return false;
        }

        public HashSet<int> GetPossibleDepths(string quotationMark, QuotationMarkDirection direction)
        {
            var depths = new HashSet<int>();
            foreach (
                (int depth, SingleLevelQuoteConvention levelConvention) in LevelConventions.Select((l, i) => (i + 1, l))
            )
            {
                if (
                    direction == QuotationMarkDirection.Opening
                    && levelConvention.OpeningQuotationMark == quotationMark
                )
                {
                    depths.Add(depth);
                }
                else if (
                    direction == QuotationMarkDirection.Closing
                    && levelConvention.ClosingQuotationMark == quotationMark
                )
                {
                    depths.Add(depth);
                }
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
            if (!openingQuotationMarks.Contains(GetOpeningQuotationMarkAtDepth(1)))
                return false;
            if (!closingQuotationMarks.Contains(GetClosingQuotationMarkAtDepth(1)))
                return false;
            return true;
        }

        public QuoteConvention Normalize()
        {
            return new QuoteConvention(Name + "_normalized", LevelConventions.Select(l => l.Normalize()).ToList());
        }
    }
}
