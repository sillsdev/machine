using System.Collections.Generic;
using System.Linq;
using PCRE;

namespace SIL.Machine.PunctuationAnalysis
{
    public class QuotationMarkFinder
    {
        private static readonly PcreRegex QuotationMarkPattern = new PcreRegex(@"(\p{Quotation_Mark}|<<|>>|<|>)");
        private readonly QuoteConventionSet _quoteConventions;

        public QuotationMarkFinder(QuoteConventionSet quoteConventions)
        {
            _quoteConventions = quoteConventions;
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

        public virtual List<QuotationMarkStringMatch> FindAllPotentialQuotationMarksInTextSegments(
            IReadOnlyList<TextSegment> textSegments
        )
        {
            return textSegments.SelectMany(ts => FindAllPotentialQuotationMarksInTextSegment(ts)).ToList();
        }

        public List<QuotationMarkStringMatch> FindAllPotentialQuotationMarksInTextSegment(TextSegment textSegment)
        {
            return QuotationMarkPattern
                .Matches(textSegment.Text)
                .Cast<PcreMatch>()
                .Where(match =>
                    _quoteConventions.IsValidOpeningQuotationMark(match.Groups[0].Value)
                    || _quoteConventions.IsValidClosingQuotationMark(match.Groups[0].Value)
                )
                .Select(m =>
                {
                    CodePointString codePointString = new CodePointString(textSegment.Text);
                    int startIndex = codePointString.GetCodePointIndexForStringIndex(m.Groups[0].Index);
                    int endIndex = codePointString.GetCodePointIndexForStringIndex(m.Groups[0].EndIndex);
                    return new QuotationMarkStringMatch(textSegment, startIndex, endIndex);
                })
                .ToList();
        }
    }
}
