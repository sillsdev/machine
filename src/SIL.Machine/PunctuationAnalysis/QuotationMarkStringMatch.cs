using System;
using System.Globalization;
using System.Text.RegularExpressions;
using PCRE;

namespace SIL.Machine.PunctuationAnalysis
{
    public class QuotationMarkStringMatch
    {
        private static readonly PcreRegex LetterPattern = new PcreRegex(@"[\p{L}\N{U+0001E200}-\N{U+0001E28F}]");
        private static readonly PcreRegex LatinLetterPattern = new PcreRegex(@"^\p{Script_Extensions=Latin}$");
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

        public override bool Equals(object obj)
        {
            if (!(obj is QuotationMarkStringMatch other))
                return false;
            return TextSegment.Equals(other.TextSegment)
                && StartIndex == other.StartIndex
                && EndIndex == other.EndIndex;
        }

        public override int GetHashCode()
        {
            int code = 23;
            code = code * 31 + TextSegment.GetHashCode();
            code = code * 31 + StartIndex.GetHashCode();
            code = code * 31 + EndIndex.GetHashCode();
            return code;
        }

        public string QuotationMark =>
            new StringInfo(TextSegment.Text).SubstringByTextElements(StartIndex, EndIndex - StartIndex);

        public bool IsValidOpeningQuotationMark(QuoteConventionSet quoteConventions) =>
            quoteConventions.IsValidOpeningQuotationMark(QuotationMark);

        public bool IsValidClosingQuotationMark(QuoteConventionSet quoteConventions) =>
            quoteConventions.IsValidClosingQuotationMark(QuotationMark);

        public bool QuotationMarkMatches(Regex regexPattern) => regexPattern.IsMatch(QuotationMark);

        public bool NextCharacterMatches(Regex regexPattern) =>
            NextCharacter != null && regexPattern.IsMatch(NextCharacter);

        public bool NextCharacterMatches(PcreRegex regexPattern) =>
            NextCharacter != null && regexPattern.IsMatch(NextCharacter);

        public bool PreviousCharacterMatches(Regex regexPattern) =>
            PreviousCharacter != null && regexPattern.IsMatch(PreviousCharacter);

        public bool PreviousCharacterMatches(PcreRegex regexPattern) =>
            PreviousCharacter != null && regexPattern.IsMatch(PreviousCharacter);

        public string PreviousCharacter
        {
            get
            {
                if (IsAtStartOfSegment)
                {
                    TextSegment previousSegment = TextSegment.PreviousSegment;
                    if (previousSegment != null && !TextSegment.MarkerIsInPrecedingContext(UsfmMarkerType.Paragraph))
                    {
                        return new StringInfo(previousSegment.Text).SubstringByTextElements(
                            previousSegment.Text.Length - 1,
                            1
                        );
                    }
                    return null;
                }
                return new StringInfo(TextSegment.Text).SubstringByTextElements(StartIndex - 1, 1);
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
                        return new StringInfo(nextSegment.Text).SubstringByTextElements(0, 1);
                    }
                    return null;
                }
                return new StringInfo(TextSegment.Text).SubstringByTextElements(EndIndex, 1);
            }
        }

        public bool LeadingSubstringMatches(Regex regexPattern) =>
            regexPattern.IsMatch(TextSegment.SubstringBefore(StartIndex));

        public bool LeadingSubstringMatches(PcreRegex regexPattern) =>
            regexPattern.IsMatch(TextSegment.SubstringBefore(StartIndex));

        public bool TrailingSubstringMatches(Regex regexPattern) =>
            regexPattern.IsMatch(TextSegment.SubstringAfter(EndIndex));

        public bool TrailingSubstringMatches(PcreRegex regexPattern) =>
            regexPattern.IsMatch(TextSegment.SubstringAfter(EndIndex));

        // This assumes that the two matches occur in the same verse
        public bool Precedes(QuotationMarkStringMatch other)
        {
            return TextSegment.IndexInVerse < other.TextSegment.IndexInVerse
                || (TextSegment.IndexInVerse == other.TextSegment.IndexInVerse && StartIndex < other.StartIndex);
        }

        // Not used, but a useful method for debugging
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
