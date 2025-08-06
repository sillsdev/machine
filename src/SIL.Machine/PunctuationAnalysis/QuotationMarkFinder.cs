using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.Machine.PunctuationAnalysis
{
    public class QuotationMarkFinder
    {
        private static readonly Regex TypewriterGuillemetsPattern = new Regex(@"(<<|>>|<|>)", RegexOptions.Compiled);
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
            TextElementEnumerator charactersEnumerator = StringInfo.GetTextElementEnumerator(textSegment.Text);
            int index = 0;
            List<QuotationMarkStringMatch> quotationMarkStringMatches = new List<QuotationMarkStringMatch>();
            while (charactersEnumerator.MoveNext())
            {
                string currentCharacterString = charactersEnumerator.Current.ToString();
                if (
                    (
                        QuotationMarkStringMatch.HasUnicodeProperty(currentCharacterString, "QUOTATION MARK")
                        || QuotationMarkStringMatch.HasUnicodeProperty(currentCharacterString, "APOSTROPHE")
                    )
                    && (
                        _quoteConventions.IsValidOpeningQuotationMark(currentCharacterString)
                        || _quoteConventions.IsValidClosingQuotationMark(currentCharacterString)
                    )
                )
                {
                    quotationMarkStringMatches.Add(new QuotationMarkStringMatch(textSegment, index, index + 1));
                }
                index++;
            }
            List<QuotationMarkStringMatch> typewriterGuillemetMatches = TypewriterGuillemetsPattern
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

            return quotationMarkStringMatches
                .Concat(typewriterGuillemetMatches)
                .OrderBy(match => match.StartIndex)
                .ToList();
        }
    }
}
