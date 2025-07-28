using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora.PunctuationAnalysis;

namespace SIL.Machine.Corpora
{
    // Determines the best strategy to take for each chapter
    public class QuotationMarkUpdateFirstPass : UsfmStructureExtractor
    {
        private readonly QuoteConvention _sourceQuoteConvention;
        private readonly QuoteConvention _targetQuoteConvention;
        private readonly QuotationMarkFinder _quotationMarkFinder;
        private readonly DepthBasedQuotationMarkResolver _quotationMarkResolver;
        public bool WillFallbackModeWork;

        public QuotationMarkUpdateFirstPass(
            QuoteConvention sourceQuoteConvention,
            QuoteConvention targetQuoteConvention
        )
        {
            _sourceQuoteConvention = sourceQuoteConvention;
            _targetQuoteConvention = targetQuoteConvention;
            _quotationMarkFinder = new QuotationMarkFinder(
                new QuoteConventionSet(new List<QuoteConvention> { sourceQuoteConvention, targetQuoteConvention })
            );
            _quotationMarkResolver = new DepthBasedQuotationMarkResolver(
                new QuotationMarkUpdateResolutionSettings(sourceQuoteConvention)
            );
            WillFallbackModeWork = CheckWhetherFallbackModeWillWork(sourceQuoteConvention, targetQuoteConvention);
        }

        public bool CheckWhetherFallbackModeWillWork(
            QuoteConvention sourceQuoteConvention,
            QuoteConvention targetQuoteConvention
        )
        {
            var targetMarksBySourceMarks = new Dictionary<string, HashSet<string>>();
            foreach (int depth in Enumerable.Range(1, sourceQuoteConvention.NumLevels))
            {
                string openingQuotationMark = sourceQuoteConvention.GetOpeningQuotationMarkAtDepth(depth);
                if (!targetMarksBySourceMarks.TryGetValue(openingQuotationMark, out HashSet<string> marks))
                {
                    marks = new HashSet<string>();
                    targetMarksBySourceMarks[openingQuotationMark] = marks;
                }
                if (depth <= targetQuoteConvention.NumLevels)
                {
                    marks.Add(targetQuoteConvention.GetClosingQuotationMarkAtDepth(depth));
                }
            }

            return !targetMarksBySourceMarks.Keys.Any(sourceMark => targetMarksBySourceMarks[sourceMark].Count > 1);
        }

        public List<QuotationMarkUpdateStrategy> FindBestChapterStrategies()
        {
            var bestActionsByChapter = new List<QuotationMarkUpdateStrategy>();
            foreach (Chapter chapter in GetChapters())
            {
                bestActionsByChapter.Add(FindBestStrategyForChapter(chapter));
            }
            return bestActionsByChapter;
        }

        public QuotationMarkUpdateStrategy FindBestStrategyForChapter(Chapter chapter)
        {
            List<QuotationMarkStringMatch> quotationMarkMatches =
                _quotationMarkFinder.FindAllPotentialQuotationMarksInChapter(chapter);

            _quotationMarkResolver.Reset();

            // Use ToList() to force evaluation of the generator
            _quotationMarkResolver.ResolveQuotationMarks(quotationMarkMatches).ToList();

            return ChooseBestStrategyBasedOnObservedIssues(_quotationMarkResolver.GetIssues());
        }

        public QuotationMarkUpdateStrategy ChooseBestStrategyBasedOnObservedIssues(
            HashSet<QuotationMarkResolutionIssue> issues
        )
        {
            if (issues.Contains(QuotationMarkResolutionIssue.AmbiguousQuotationMark))
                return QuotationMarkUpdateStrategy.Skip;

            if (
                issues.Contains(QuotationMarkResolutionIssue.UnpairedQuotationMark)
                || issues.Contains(QuotationMarkResolutionIssue.TooDeepNesting)
            )
            {
                if (WillFallbackModeWork)
                    return QuotationMarkUpdateStrategy.ApplyFallback;
                return QuotationMarkUpdateStrategy.Skip;
            }
            return QuotationMarkUpdateStrategy.ApplyFull;
        }
    }
}
