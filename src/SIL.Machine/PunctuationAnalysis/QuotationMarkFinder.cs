using System.Collections.Generic;
using System.Globalization;
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
                    int[] textElementIndices = StringInfo.ParseCombiningCharacters(textSegment.Text);
                    int startIndex = 0;
                    int endIndex = textElementIndices.Length;
                    for (int textElementIndex = 0; textElementIndex < textElementIndices.Length; textElementIndex++)
                    {
                        int stringIndex = textElementIndices[textElementIndex];
                        if (stringIndex == m.Groups[0].Index)
                            startIndex = textElementIndex;
                        if (stringIndex == m.Groups[0].EndIndex)
                            endIndex = textElementIndex;
                    }
                    return new QuotationMarkStringMatch(textSegment, startIndex, endIndex);
                })
                .ToList();
        }
    }
}
