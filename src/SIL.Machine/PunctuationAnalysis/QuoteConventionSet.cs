using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using SIL.Extensions;

namespace SIL.Machine.PunctuationAnalysis
{
    public class QuoteConventionSet : IEquatable<QuoteConventionSet>
    {
        public IReadOnlyList<QuoteConvention> Conventions { get; }

        public Regex OpeningQuotationMarkRegex { get; private set; }
        public Regex ClosingQuotationMarkRegex { get; private set; }
        public Regex AllQuotationMarkRegex { get; private set; }

        public IReadOnlyDictionary<string, HashSet<string>> ClosingMarksByOpeningMark { get; private set; }
        public IReadOnlyDictionary<string, HashSet<string>> OpeningMarksByClosingMark { get; private set; }

        public QuoteConventionSet(List<QuoteConvention> conventions)
        {
            Conventions = conventions;
            CreateQuotationMarkRegexes();
            CreateQuotationMarkPairMap();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is QuoteConventionSet other))
                return false;
            return Equals(other);
        }

        public bool Equals(QuoteConventionSet other)
        {
            return Conventions.SequenceEqual(other.Conventions);
        }

        public override int GetHashCode()
        {
            int hashCode = 23;
            return hashCode * 31 + Conventions.GetHashCode();
        }

        private void CreateQuotationMarkRegexes()
        {
            OpeningQuotationMarkRegex = new Regex(@"", RegexOptions.Compiled);
            ClosingQuotationMarkRegex = new Regex(@"", RegexOptions.Compiled);
            AllQuotationMarkRegex = new Regex(@"", RegexOptions.Compiled);

            var openingQuotationMarks = new HashSet<string>();
            var closingQuotationMarks = new HashSet<string>();

            foreach (QuoteConvention convention in Conventions)
            {
                for (int level = 1; level < convention.NumLevels + 1; level++)
                {
                    string openingQuote = convention.GetOpeningQuotationMarkAtDepth(level);
                    string closingQuote = convention.GetClosingQuotationMarkAtDepth(level);
                    openingQuotationMarks.Add(openingQuote);
                    closingQuotationMarks.Add(closingQuote);
                }
            }

            var allQuotationMarks = openingQuotationMarks.Union(closingQuotationMarks).ToImmutableHashSet();

            if (allQuotationMarks.Count > 0)
            {
                OpeningQuotationMarkRegex = new Regex(
                    @"[" + string.Join("", openingQuotationMarks.OrderBy(q => q)) + "]",
                    RegexOptions.Compiled
                );
                ClosingQuotationMarkRegex = new Regex(
                    @"[" + string.Join("", closingQuotationMarks.OrderBy(q => q)) + "]",
                    RegexOptions.Compiled
                );
                AllQuotationMarkRegex = new Regex(
                    @"[" + string.Join("", allQuotationMarks.OrderBy(q => q)) + "]",
                    RegexOptions.Compiled
                );
            }
        }

        private void CreateQuotationMarkPairMap()
        {
            var closingMarksByOpeningMark = new Dictionary<string, HashSet<string>>();
            var openingMarksByClosingMark = new Dictionary<string, HashSet<string>>();
            foreach (QuoteConvention convention in Conventions)
            {
                for (int level = 1; level < convention.NumLevels + 1; level++)
                {
                    string openingQuote = convention.GetOpeningQuotationMarkAtDepth(level);
                    string closingQuote = convention.GetClosingQuotationMarkAtDepth(level);
                    closingMarksByOpeningMark.UpdateValue(
                        openingQuote,
                        () => new HashSet<string>(),
                        set =>
                        {
                            set.Add(closingQuote);
                            return set;
                        }
                    );
                    openingMarksByClosingMark.UpdateValue(
                        closingQuote,
                        () => new HashSet<string>(),
                        set =>
                        {
                            set.Add(openingQuote);
                            return set;
                        }
                    );
                }
            }
            ClosingMarksByOpeningMark = closingMarksByOpeningMark;
            OpeningMarksByClosingMark = openingMarksByClosingMark;
        }

        public QuoteConvention GetQuoteConventionByName(string name)
        {
            foreach (QuoteConvention convention in Conventions)
            {
                if (convention.Name == name)
                {
                    return convention;
                }
            }
            return null;
        }

        public IReadOnlyList<string> GetAllQuoteConventionNames()
        {
            return Conventions.Select(c => c.Name).OrderBy(c => c).ToList();
        }

        public IReadOnlyList<string> GetPossibleOpeningQuotationMarks()
        {
            return ClosingMarksByOpeningMark.Keys.OrderBy(k => k).ToList();
        }

        public IReadOnlyList<string> GetPossibleClosingQuotationMarks()
        {
            return OpeningMarksByClosingMark.Keys.OrderBy(k => k).ToList();
        }

        public bool IsValidOpeningQuotationMark(string quotationMark)
        {
            return ClosingMarksByOpeningMark.ContainsKey(quotationMark);
        }

        public bool IsValidClosingQuotationMark(string quotationMark)
        {
            return OpeningMarksByClosingMark.ContainsKey(quotationMark);
        }

        public bool MarksAreAValidPair(string openingMark, string closingMark)
        {
            return ClosingMarksByOpeningMark.TryGetValue(openingMark, out HashSet<string> set)
                && set.Contains(closingMark);
        }

        public bool IsQuotationMarkDirectionAmbiguous(string quotationMark)
        {
            return (
                ClosingMarksByOpeningMark.TryGetValue(quotationMark, out HashSet<string> closingMarks)
                && closingMarks.Contains(quotationMark)
            );
        }

        public HashSet<string> GetPossiblePairedQuotationMarks(string quotationMark)
        {
            var pairedQuotationMarks = new HashSet<string>();
            if (ClosingMarksByOpeningMark.TryGetValue(quotationMark, out HashSet<string> set))
            {
                pairedQuotationMarks.AddRange(set);
            }
            if (OpeningMarksByClosingMark.TryGetValue(quotationMark, out set))
            {
                pairedQuotationMarks.AddRange(set);
            }
            return pairedQuotationMarks;
        }

        public HashSet<int> GetPossibleDepths(string quotationMark, QuotationMarkDirection direction)
        {
            var depths = new HashSet<int>();
            foreach (QuoteConvention convention in Conventions)
            {
                depths.AddRange(convention.GetPossibleDepths(quotationMark, direction));
            }
            return depths;
        }

        public bool MetadataMatchesQuotationMark(string quotationMark, int depth, QuotationMarkDirection direction)
        {
            foreach (QuoteConvention convention in Conventions)
            {
                if (convention.GetExpectedQuotationMark(depth, direction) == quotationMark)
                    return true;
            }
            return false;
        }

        public QuoteConventionSet FilterToCompatibleQuoteConventions(
            List<string> openingQuotationMarks,
            List<string> closingQuotationMarks
        )
        {
            return new QuoteConventionSet(
                Conventions
                    .Where(c => c.IsCompatibleWithObservedQuotationMarks(openingQuotationMarks, closingQuotationMarks))
                    .ToList()
            );
        }

        public (QuoteConvention Convention, double Similarity) FindMostSimilarConvention(
            QuotationMarkTabulator tabulatedQuotationMarks
        )
        {
            double bestSimilarity = double.MinValue;
            QuoteConvention bestQuoteConvention = null;
            foreach (QuoteConvention quoteConvention in Conventions)
            {
                double similarity = tabulatedQuotationMarks.CalculateSimilarity(quoteConvention);
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestQuoteConvention = quoteConvention;
                }
            }
            return (bestQuoteConvention, bestSimilarity);
        }
    }
}
