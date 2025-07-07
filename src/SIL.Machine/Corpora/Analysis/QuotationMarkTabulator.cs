using System;
using System.Collections.Generic;
using SIL.Extensions;

namespace SIL.Machine.Corpora.Analysis
{
    public class QuotationMarkCounts
    {
        private readonly Dictionary<string, int> _stringCounts;

        public int TotalCount { get; private set; }

        public QuotationMarkCounts()
        {
            _stringCounts = new Dictionary<string, int>();
            TotalCount = 0;
        }

        public void CountQuotationMark(string quotationMark)
        {
            if (!_stringCounts.ContainsKey(quotationMark))
            {
                _stringCounts[quotationMark] = 0;
            }
            _stringCounts[quotationMark]++;
            TotalCount++;
        }

        public (string BestString, int BestStringCount, int TotalStringCount) FindBestQuotationMarkProportion()
        {
            string bestString = _stringCounts.MaxBy(kvp => kvp.Value).Key;
            return (bestString, _stringCounts[bestString], TotalCount);
        }

        public int CalculateNumDifferences(string expectedQuotationMark)
        {
            if (!_stringCounts.TryGetValue(expectedQuotationMark, out int count))
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
            if (!_quotationCountsByDepthAndDirection.ContainsKey(key))
            {
                _quotationCountsByDepthAndDirection[key] = new QuotationMarkCounts();
            }
            _quotationCountsByDepthAndDirection[key].CountQuotationMark(quotationMark);
        }

        // Used in print function
        // private bool DepthAndDirectionObserved(int depth, QuotationMarkDirection direction) =>
        //     _quotationCountsByDepthAndDirection.ContainsKey((depth, direction));

        // private (
        //     string BestQuotationMark,
        //     int BestQuotationMarkCount,
        //     int TotalQuotationMarkCount
        // ) FindMostCommonQuotationMarkWithDepthAndDirection(int depth, QuotationMarkDirection direction)
        // {
        //     return _quotationCountsByDepthAndDirection[(depth, direction)].FindBestQuotationMarkProportion();
        // }

        public double CalculateSimilarity(QuoteConvention quoteConvention)
        {
            double numDifferences = 0.0;
            double numTotalQuotationMarks = 0.0;
            foreach ((int depth, QuotationMarkDirection direction) in _quotationCountsByDepthAndDirection.Keys)
            {
                string expectedQuotationMark = quoteConvention.GetExpectedQuotationMark(depth, direction);

                // give higher weight to shallower depths, since deeper marks are more likely to be mistakes
                numDifferences += (
                    _quotationCountsByDepthAndDirection[(depth, direction)]
                        .CalculateNumDifferences(expectedQuotationMark) * Math.Pow(2, -depth)
                );
                numTotalQuotationMarks +=
                    _quotationCountsByDepthAndDirection[(depth, direction)].TotalCount * Math.Pow(2, -depth);
            }
            if (numTotalQuotationMarks == 0.0)
            {
                return 0.0;
            }
            return 1 - (numDifferences / numTotalQuotationMarks);
        }
    }
}
