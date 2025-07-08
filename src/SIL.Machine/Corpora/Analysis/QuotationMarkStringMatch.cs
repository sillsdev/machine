using System;
using System.Text.RegularExpressions;

namespace SIL.Machine.Corpora.Analysis
{
    public class QuotationMarkStringMatch
    {
        private static readonly Regex LetterPattern = new Regex(@"[\p{L}\uD838[\uDE00-\uDE8F]]", RegexOptions.Compiled);
        private static readonly Regex LatinLetterPattern = new Regex(@"^\p{IsBasicLatin}$", RegexOptions.Compiled);
        private static readonly Regex WhitespacePattern = new Regex(@"[\s~]", RegexOptions.Compiled);
        private static readonly Regex PunctuationPattern = new Regex(@"[\.,;\?!\)\]\-—۔،؛]", RegexOptions.Compiled);
        private static readonly Regex QuoteIntroducerPattern = new Regex(@"[:,]\s*$", RegexOptions.Compiled);

        public TextSegment TextSegment { get; }
        public int StartIndex { get; }
        public int EndIndex { get; }

        public QuotationMarkStringMatch(TextSegment textSegment, int startIndex, int endIndex)
        {
            TextSegment = textSegment;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        public string QuotationMark => TextSegment.Text.Substring(StartIndex, EndIndex - StartIndex);

        public bool IsValidOpeningQuotationMark(QuoteConventionSet quoteConventionSet) =>
            quoteConventionSet.IsValidOpeningQuotationMark(QuotationMark);

        public bool IsValidClosingQuotationMark(QuoteConventionSet quoteConventionSet) =>
            quoteConventionSet.IsValidClosingQuotationMark(QuotationMark);

        public bool QuotationMarkMatches(Regex regexPattern) => regexPattern.IsMatch(QuotationMark);

        public bool NextCharacterMatches(Regex regexPattern) =>
            NextCharacter != null && regexPattern.IsMatch(NextCharacter);

        public bool PreviousCharacterMatches(Regex regexPattern) =>
            PreviousCharacter != null && regexPattern.IsMatch(PreviousCharacter);

        public string PreviousCharacter
        {
            get
            {
                if (StartIndex == 0)
                {
                    TextSegment previousSegment = TextSegment.PreviousSegment;
                    if (previousSegment != null && !TextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Paragraph))
                    {
                        return previousSegment.Text[previousSegment.Text.Length - 1].ToString();
                    }
                    return null;
                }
                return TextSegment.Text[StartIndex - 1].ToString();
            }
        }

        public string NextCharacter
        {
            get
            {
                if (IsAtEndOfSegment)
                {
                    TextSegment nextSegment = TextSegment.NextSegment;
                    if (nextSegment != null && !TextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Paragraph))
                    {
                        return nextSegment.Text[0].ToString();
                    }
                    return null;
                }
                return TextSegment.Text[EndIndex].ToString();
            }
        }

        public bool LeadingSubstringMatches(Regex regexPattern) =>
            regexPattern.IsMatch(TextSegment.SubstringBefore(StartIndex));

        public bool TrailingSubstringMatches(Regex regexPattern) =>
            regexPattern.IsMatch(TextSegment.SubstringAfter(EndIndex));

        // this assumes that the two matches occur in the same verse
        public bool Precedes(QuotationMarkStringMatch other)
        {
            return TextSegment.IndexInVerse < other.TextSegment.IndexInVerse
                || (TextSegment.IndexInVerse == other.TextSegment.IndexInVerse && StartIndex < other.StartIndex);
        }

        // not used, but a useful method for debugging
        public string Context()
        {
            int contextStartIndex = Math.Max(StartIndex - 10, 0);
            int contextEndIndex = Math.Min(EndIndex + 10, TextSegment.Length);
            return TextSegment.Text.Substring(contextStartIndex, contextEndIndex - contextStartIndex);
        }

        public QuotationMarkMetadata Resolve(int depth, QuotationMarkDirection direction) =>
            new QuotationMarkMetadata(QuotationMark, depth, direction, TextSegment, StartIndex, EndIndex);

        public bool IsAtStartOfSegment => StartIndex == 0;

        public bool IsAtEndOfSegment => EndIndex == TextSegment.Length;

        public bool HasLeadingWhitespace()
        {
            if (PreviousCharacter == null)
            {
                return TextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Paragraph)
                    || TextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Embed)
                    || TextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Verse);
            }
            return PreviousCharacterMatches(WhitespacePattern);
        }

        public bool HasTrailingWhitespace()
        {
            return NextCharacterMatches(WhitespacePattern);
        }

        public bool HasLeadingPunctuation()
        {
            return PreviousCharacterMatches(PunctuationPattern);
        }

        public bool HasTrailingPunctuation()
        {
            return NextCharacterMatches(PunctuationPattern);
        }

        public bool HasLetterInLeadingSubstring()
        {
            return LeadingSubstringMatches(LetterPattern);
        }

        public bool HasLetterInTrailingSubstring()
        {
            return TrailingSubstringMatches(LetterPattern);
        }

        public bool HasLeadingLatinLetter()
        {
            return PreviousCharacterMatches(LatinLetterPattern);
        }

        public bool HasTrailingLatinLetter()
        {
            return NextCharacterMatches(LatinLetterPattern);
        }

        public bool HasQuoteIntroducerInLeadingSubstring()
        {
            return LeadingSubstringMatches(QuoteIntroducerPattern);
        }
    }
}
