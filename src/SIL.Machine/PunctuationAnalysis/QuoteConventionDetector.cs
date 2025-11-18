using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.PunctuationAnalysis
{
    public class QuoteConventionDetector : UsfmStructureExtractor
    {
        private readonly QuotationMarkTabulator _quotationMarkTabulator;

        public QuoteConventionDetector()
            : base()
        {
            _quotationMarkTabulator = new QuotationMarkTabulator();
        }

        private void CountQuotationMarksInChapters(List<Chapter> chapters)
        {
            QuoteConventionSet possibleQuoteConventions = new PreliminaryQuotationMarkAnalyzer(
                QuoteConventions.Standard
            ).NarrowDownPossibleQuoteConventions(chapters);

            foreach (Chapter chapter in chapters)
                CountQuotationMarksInChapter(chapter, possibleQuoteConventions);
        }

        private void CountQuotationMarksInChapter(Chapter chapter, QuoteConventionSet possibleQuoteConventions)
        {
            List<QuotationMarkStringMatch> quotationMarkMatches = new QuotationMarkFinder(
                possibleQuoteConventions
            ).FindAllPotentialQuotationMarksInChapter(chapter);

            List<QuotationMarkMetadata> resolvedQuotationMarks = new DepthBasedQuotationMarkResolver(
                new QuoteConventionDetectionResolutionSettings(possibleQuoteConventions)
            )
                .ResolveQuotationMarks(quotationMarkMatches)
                .ToList();

            _quotationMarkTabulator.Tabulate(resolvedQuotationMarks);
        }

        public QuoteConventionAnalysis DetectQuoteConvention(IReadOnlyDictionary<int, List<int>> includeChapters = null)
        {
            CountQuotationMarksInChapters(GetChapters(includeChapters));

            return QuoteConventions.Standard.ScoreAllQuoteConventions(_quotationMarkTabulator);
        }
    }
}
