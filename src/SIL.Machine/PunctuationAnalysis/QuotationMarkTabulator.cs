using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Extensions;

namespace SIL.Machine.PunctuationAnalysis
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

        public void CountFrom(QuotationMarkCounts quotationMarkCounts)
        {
            foreach (KeyValuePair<string, int> kvp in quotationMarkCounts._quotationMarkCounter)
            {
                (string quotationMark, int count) = (kvp.Key, kvp.Value);
                _quotationMarkCounter.UpdateValue(quotationMark, () => 0, i => i + count);
            }
            TotalCount += quotationMarkCounts.TotalCount;
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

        public void TabulateFrom(QuotationMarkTabulator tabulatedQuotationMarks)
        {
            foreach (
                (
                    (int depth, QuotationMarkDirection direction),
                    QuotationMarkCounts otherCounts
                ) in tabulatedQuotationMarks._quotationCountsByDepthAndDirection.Select(kvp => (kvp.Key, kvp.Value))
            )
            {
                if (
                    !_quotationCountsByDepthAndDirection.TryGetValue(
                        (depth, direction),
                        out QuotationMarkCounts thisCounts
                    )
                )
                {
                    thisCounts = new QuotationMarkCounts();
                    _quotationCountsByDepthAndDirection[(depth, direction)] = thisCounts;
                }
                thisCounts.CountFrom(otherCounts);
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

        public int GetTotalQuotationMarkCount()
        {
            return _quotationCountsByDepthAndDirection.Values.Select(c => c.TotalCount).Sum();
        }

        public double CalculateSimilarity(QuoteConvention quoteConvention)
        {
            var numMarksByDepth = new Dictionary<int, int>();
            var numMatchingMarksByDepth = new Dictionary<int, int>();
            foreach (
                (int depth, QuotationMarkDirection direction) in _quotationCountsByDepthAndDirection.Keys.OrderBy(k =>
                    k
                )
            )
            {
                string expectedQuotationMark = quoteConvention.GetExpectedQuotationMark(depth, direction);
                int numMatchingMarks = _quotationCountsByDepthAndDirection[(depth, direction)].TotalCount;
                numMarksByDepth.UpdateValue(depth, () => 0, i => i + numMatchingMarks);
                numMatchingMarksByDepth.UpdateValue(
                    depth,
                    () => 0,
                    i =>
                        i
                        + numMatchingMarks
                        - _quotationCountsByDepthAndDirection[(depth, direction)]
                            .CalculateNumDifferences(expectedQuotationMark)
                );
            }

            // The scores of greater depths depend on the scores of shallower depths
            var scoresByDepth = new Dictionary<int, double>();
            foreach (int depth in numMarksByDepth.Keys.OrderBy(k => k))
            {
                double previousDepthScore = 1;
                if (scoresByDepth.TryGetValue(depth - 1, out double score))
                {
                    previousDepthScore = score / numMarksByDepth[depth - 1];
                }
                scoresByDepth[depth] = previousDepthScore * numMatchingMarksByDepth[depth];
            }
            int totalMarks = numMarksByDepth.Values.Sum();
            double totalScore = scoresByDepth.Values.Sum();

            if (totalMarks == 0)
                return 0;
            return totalScore / totalMarks;
        }

        private bool DepthAndDirectionObserved(int depth, QuotationMarkDirection direction)
        {
            return _quotationCountsByDepthAndDirection.ContainsKey((depth, direction));
        }

        private (
            string openingQuotationMark,
            int observedOpeningCount,
            int totalOpeningCount
        ) FindMostCommonQuotationMarkWithDepthAndDirection(int depth, QuotationMarkDirection direction)
        {
            return _quotationCountsByDepthAndDirection.TryGetValue((depth, direction), out QuotationMarkCounts counts)
                ? counts.FindBestQuotationMarkProportion()
                : (null, 0, 0);
        }

        public string GetSummaryMessage()
        {
            var message = new StringBuilder();
            for (int depth = 1; depth < 5; depth++)
            {
                (string openingQuotationMark, int observedOpeningCount, int totalOpeningCount) =
                    FindMostCommonQuotationMarkWithDepthAndDirection(depth, QuotationMarkDirection.Opening);
                (string closingQuotationMark, int observedClosingCount, int totalClosingCount) =
                    FindMostCommonQuotationMarkWithDepthAndDirection(depth, QuotationMarkDirection.Closing);

                if (
                    DepthAndDirectionObserved(depth, QuotationMarkDirection.Opening)
                    && DepthAndDirectionObserved(depth, QuotationMarkDirection.Closing)
                )
                {
                    message.AppendLine(
                        $"The most common level {depth} quotation marks are {openingQuotationMark} ({observedOpeningCount} of {totalOpeningCount} opening marks) and {closingQuotationMark} ({observedClosingCount} of {totalClosingCount} closing marks)"
                    );
                }
            }
            return message.ToString();
        }
    }
}
