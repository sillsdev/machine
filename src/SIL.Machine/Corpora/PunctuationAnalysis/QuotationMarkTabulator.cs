using System;
using System.Collections.Generic;
using SIL.Extensions;

namespace SIL.Machine.Corpora.PunctuationAnalysis
{
    public class QuotationMarkCounts
    {
        private readonly Dictionary<string, int> _quotationMarkCounter;

        public int TotalCount { get; private set; }

        public QuotationMarkCounts()
        {
            _quotationMarkCounter = new Dictionary<string, int>();
            TotalCount = 0;
        }

        public void CountQuotationMark(string quotationMark)
        {
            _quotationMarkCounter.UpdateValue(quotationMark, () => 0, i => i + 1);
            TotalCount++;
        }

        public (string BestString, int BestStringCount, int TotalStringCount) FindBestQuotationMarkProportion()
        {
            string bestString = _quotationMarkCounter.MaxBy(kvp => kvp.Value).Key;
            return (bestString, _quotationMarkCounter[bestString], TotalCount);
        }

        public int CalculateNumDifferences(string expectedQuotationMark)
        {
            if (!_quotationMarkCounter.TryGetValue(expectedQuotationMark, out int count))
            {
                return TotalCount;
            }
            return TotalCount - count;
        }
    }

    public class QuotationMarkTabulator
    {
        private readonly Dictionary<
            (int Depth, QuotationMarkDirection Direction),
            QuotationMarkCounts
        > _quotationCountsByDepthAndDirection;

        public QuotationMarkTabulator()
        {
            _quotationCountsByDepthAndDirection =
                new Dictionary<(int Depth, QuotationMarkDirection Direction), QuotationMarkCounts>();
        }

        public void Tabulate(List<QuotationMarkMetadata> quotationMarks)
        {
            foreach (QuotationMarkMetadata quotationMark in quotationMarks)
            {
                CountQuotationMark(quotationMark);
            }
        }

        private void CountQuotationMark(QuotationMarkMetadata quote)
        {
            (int Depth, QuotationMarkDirection Direction) key = (quote.Depth, quote.Direction);
            string quotationMark = quote.QuotationMark;
            _quotationCountsByDepthAndDirection.UpdateValue(
                key,
                () => new QuotationMarkCounts(),
                counts =>
                {
                    counts.CountQuotationMark(quotationMark);
                    return counts;
                }
            );
        }

        public double CalculateSimilarity(QuoteConvention quoteConvention)
        {
            double weightedDifference = 0.0;
            double totalWeight = 0.0;
            foreach ((int depth, QuotationMarkDirection direction) in _quotationCountsByDepthAndDirection.Keys)
            {
                string expectedQuotationMark = quoteConvention.GetExpectedQuotationMark(depth, direction);

                // give higher weight to shallower depths, since deeper marks are more likely to be mistakes
                weightedDifference += (
                    _quotationCountsByDepthAndDirection[(depth, direction)]
                        .CalculateNumDifferences(expectedQuotationMark) * Math.Pow(2, -depth)
                );
                totalWeight += _quotationCountsByDepthAndDirection[(depth, direction)].TotalCount * Math.Pow(2, -depth);
            }
            if (totalWeight == 0.0)
            {
                return 0.0;
            }
            return 1 - (weightedDifference / totalWeight);
        }
    }
}
