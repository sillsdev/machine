using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.Machine.Corpora.PunctuationAnalysis
{
    public class QuotationMarkFinder
    {
        private static readonly Regex QuotationMarkPattern = new Regex(
            @"(\p{Pi}|\p{Pf}|<<|>>|<|>)",
            RegexOptions.Compiled
        );
        private readonly QuoteConventionSet _quoteConventions;

        public QuotationMarkFinder(QuoteConventionSet quoteConventionSet)
        {
            _quoteConventions = quoteConventionSet;
        }

        public List<QuotationMarkStringMatch> FindAllPotentialQuotationMarksInChapter(Chapter chapter)
        {
            var quotationMatches = new List<QuotationMarkStringMatch>();
            foreach (Verse verse in chapter.Verses)
                quotationMatches.AddRange(FindAllPotentialQuotationMarksInVerse(verse));
            return quotationMatches;
        }

        public List<QuotationMarkStringMatch> FindAllPotentialQuotationMarksInVerse(Verse verse)
        {
            return FindAllPotentialQuotationMarksInTextSegments(verse.TextSegments);
        }

        public List<QuotationMarkStringMatch> FindAllPotentialQuotationMarksInTextSegments(
            List<TextSegment> textSegments
        )
        {
            return textSegments.SelectMany(ts => FindAllPotentialQuotationMarksInTextSegment(ts)).ToList();
        }

        public List<QuotationMarkStringMatch> FindAllPotentialQuotationMarksInTextSegment(TextSegment textSegment)
        {
            return QuotationMarkPattern
                .Matches(textSegment.Text)
                .Cast<Match>()
                .Where(match =>
                    _quoteConventions.IsValidOpeningQuotationMark(match.Groups[0].Value)
                    || _quoteConventions.IsValidClosingQuotationMark(match.Groups[0].Value)
                )
                .Select(m => new QuotationMarkStringMatch(
                    textSegment,
                    m.Groups[0].Index,
                    m.Groups[0].Index + m.Groups[0].Length
                ))
                .ToList();
        }
    }
}
