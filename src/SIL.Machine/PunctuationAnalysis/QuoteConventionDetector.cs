using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.PunctuationAnalysis
{
    public class QuoteConventionAnalysis
    {
        public QuoteConvention BestQuoteConvention { get; private set; }
        public double BestQuoteConventionScore { get; private set; }
        public string AnalysisSummary { get; private set; }

        public QuoteConventionAnalysis(
            QuoteConvention bestQuoteConvention,
            double bestQuoteConventionScore,
            string analysisSummary
        )
        {
            BestQuoteConvention = bestQuoteConvention;
            BestQuoteConventionScore = bestQuoteConventionScore;
            AnalysisSummary = analysisSummary;
        }
    }

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
                StandardQuoteConventions.QuoteConventions
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

        public QuoteConventionAnalysis DetectQuotationConvention()
        {
            CountQuotationMarksInChapters(GetChapters());

            (QuoteConvention bestQuoteConvention, double score) =
                StandardQuoteConventions.QuoteConventions.FindMostSimilarConvention(_quotationMarkTabulator);

            if (score > 0 && bestQuoteConvention != null)
            {
                return new QuoteConventionAnalysis(
                    bestQuoteConvention,
                    score,
                    _quotationMarkTabulator.GetSummaryMessage()
                );
            }
            return null;
        }
    }
}
