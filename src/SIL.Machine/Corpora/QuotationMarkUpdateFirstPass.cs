using System;
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
            var targetMarkBySourceMark = new Dictionary<string, string>();
            foreach (
                int depth in Enumerable.Range(
                    1,
                    Math.Min(sourceQuoteConvention.NumLevels, targetQuoteConvention.NumLevels)
                )
            )
            {
                string openingQuotationMark = sourceQuoteConvention.GetOpeningQuotationMarkAtDepth(depth);
                string closingQuotationMark = targetQuoteConvention.GetClosingQuotationMarkAtDepth(depth);
                if (
                    targetMarkBySourceMark.TryGetValue(
                        openingQuotationMark,
                        out string correspondingClosingQuotationMark
                    )
                    && correspondingClosingQuotationMark != closingQuotationMark
                )
                {
                    return false;
                }
                targetMarkBySourceMark[openingQuotationMark] = closingQuotationMark;
            }
            return true;
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
