using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;

namespace SIL.Machine.PunctuationAnalysis
{
    public class QuoteConventionAnalysis
    {
        public QuoteConvention BestQuoteConvention { get; private set; }
        public double BestQuoteConventionScore { get; private set; }
        public string AnalysisSummary { get; private set; }
        public IReadOnlyDictionary<QuoteConvention, double> ConventionScores { get; private set; }
        public QuotationMarkTabulator TabulatedQuotationMarks { get; private set; }
        public double AnalysisWeight { get; private set; }

        public QuoteConventionAnalysis(
            Dictionary<QuoteConvention, double> conventionScores,
            QuotationMarkTabulator tabulatedQuotationMarks,
            double analysisWeight = 1.0
        )
        {
            ConventionScores = conventionScores;
            if (ConventionScores.Count > 0)
            {
                KeyValuePair<QuoteConvention, double> maxKvp = ConventionScores.MaxBy(kvp => kvp.Value);
                (BestQuoteConvention, BestQuoteConventionScore) = (maxKvp.Key, maxKvp.Value);
            }
            else
            {
                BestQuoteConventionScore = 0;
                BestQuoteConvention = null;
            }
            TabulatedQuotationMarks = tabulatedQuotationMarks;
            AnalysisWeight = analysisWeight;
        }

        public class Builder
        {
            public Dictionary<QuoteConvention, double> ConventionScores { get; private set; }
            public QuotationMarkTabulator TabulatedQuotationMarks { get; private set; }

            public Builder(QuotationMarkTabulator tabulatedQuotationMarks)
            {
                ConventionScores = new Dictionary<QuoteConvention, double>();
                TabulatedQuotationMarks = tabulatedQuotationMarks;
            }

            public void RecordConventionScore(QuoteConvention quoteConvention, double score)
            {
                ConventionScores[quoteConvention] = score;
            }

            public QuoteConventionAnalysis Build()
            {
                return new QuoteConventionAnalysis(
                    ConventionScores,
                    TabulatedQuotationMarks,
                    TabulatedQuotationMarks.GetTotalQuotationMarkCount()
                );
            }
        }

        public static QuoteConventionAnalysis CombineWithWeightedAverage(
            List<QuoteConventionAnalysis> quoteConventionAnalyses
        )
        {
            double totalWeight = 0;
            Dictionary<string, double> conventionVotes = new Dictionary<string, double>();
            Dictionary<string, QuoteConvention> quoteConventionsByName = new Dictionary<string, QuoteConvention>();
            QuotationMarkTabulator totalTabulatedQuotationMarks = new QuotationMarkTabulator();
            foreach (QuoteConventionAnalysis quoteConventionAnalysis in quoteConventionAnalyses)
            {
                totalTabulatedQuotationMarks.TabulateFrom(quoteConventionAnalysis.TabulatedQuotationMarks);
                totalWeight += quoteConventionAnalysis.AnalysisWeight;
                foreach (
                    (QuoteConvention convention, double score) in quoteConventionAnalysis.ConventionScores.Select(kvp =>
                        (kvp.Key, kvp.Value)
                    )
                )
                {
                    quoteConventionsByName[convention.Name] = convention;
                    conventionVotes.UpdateValue(
                        convention.Name,
                        () => 0,
                        s => s + score * quoteConventionAnalysis.AnalysisWeight
                    );
                }
            }
            QuoteConventionAnalysis.Builder builder = new QuoteConventionAnalysis.Builder(totalTabulatedQuotationMarks);
            foreach ((string conventionName, double totalScore) in conventionVotes.Select(kvp => (kvp.Key, kvp.Value)))
            {
                if (totalScore > 0)
                {
                    builder.RecordConventionScore(quoteConventionsByName[conventionName], totalScore / totalWeight);
                }
            }
            return builder.Build();
        }
    }
}
