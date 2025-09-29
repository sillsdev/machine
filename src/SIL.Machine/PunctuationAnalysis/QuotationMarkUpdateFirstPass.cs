namespace SIL.Machine.PunctuationAnalysis
{
    // Determines the best strategy to take for each chapter
    public class QuotationMarkUpdateFirstPass : UsfmStructureExtractor
    {
        private readonly QuotationMarkFinder _quotationMarkFinder;
        private readonly DepthBasedQuotationMarkResolver _quotationMarkResolver;
        public bool WillFallbackModeWork { get; set; }

        public QuotationMarkUpdateFirstPass(QuoteConvention oldQuoteConvention, QuoteConvention newQuoteConvention)
        {
            _quotationMarkFinder = new QuotationMarkFinder(
                new QuoteConventionSet(new List<QuoteConvention> { oldQuoteConvention, newQuoteConvention })
            );
            _quotationMarkResolver = new DepthBasedQuotationMarkResolver(
                new QuotationMarkUpdateResolutionSettings(oldQuoteConvention)
            );
            WillFallbackModeWork = CheckWhetherFallbackModeWillWork(oldQuoteConvention, newQuoteConvention);
        }

        public bool CheckWhetherFallbackModeWillWork(
            QuoteConvention oldQuoteConvention,
            QuoteConvention newQuoteConvention
        )
        {
            var newMarkByOldMark = new Dictionary<string, string>();
            foreach (
                int depth in Enumerable.Range(1, Math.Min(oldQuoteConvention.NumLevels, newQuoteConvention.NumLevels))
            )
            {
                string openingQuotationMark = oldQuoteConvention.GetOpeningQuotationMarkAtDepth(depth);
                string closingQuotationMark = newQuoteConvention.GetClosingQuotationMarkAtDepth(depth);
                if (
                    newMarkByOldMark.TryGetValue(openingQuotationMark, out string correspondingClosingQuotationMark)
                    && correspondingClosingQuotationMark != closingQuotationMark
                )
                {
                    return false;
                }
                newMarkByOldMark[openingQuotationMark] = closingQuotationMark;
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
