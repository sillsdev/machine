using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.Machine.Corpora.Analysis
{
    public class QuotationMarkFinder
    {
        private static readonly Regex QuotePattern = new Regex(@"(\p{Pi}|\p{Pf}|<<|>>|<|>)", RegexOptions.Compiled);
        private readonly QuoteConventionSet _quoteConventionSet;

        public QuotationMarkFinder(QuoteConventionSet quoteConventionSet)
        {
            _quoteConventionSet = quoteConventionSet;
        }

        public List<QuotationMarkStringMatch> FindAllPotentialQuotationMarksInChapter(Chapter chapter)
        {
            var quotationMatches = new List<QuotationMarkStringMatch>();
            foreach (Verse verse in chapter.Verses)
                quotationMatches.AddRange(FindAllPotentialQuotationMarksInVerse(verse));
            return quotationMatches;
        }

        public List<QuotationMarkStringMatch> FindAllPotentialQuotationMarksInVerse(Verse verse) //TODO excessive?
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
            return QuotePattern
                .Matches(textSegment.Text)
                .Cast<Match>()
                .Where(match =>
                    _quoteConventionSet.IsValidOpeningQuotationMark(match.Groups[0].Value)
                    || _quoteConventionSet.IsValidClosingQuotationMark(match.Groups[0].Value)
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
