using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Unicode;

namespace SIL.Machine.PunctuationAnalysis
{
    public class QuotationMarkStringMatch
    {
        // No LatinLetterPattern or LetterPattern because C# does not support it in the same way as Python. Using UnicodeInfo to mirror machine.py
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

        public bool PreviousCharacterMatches(Regex regexPattern) =>
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

        public bool TrailingSubstringMatches(Regex regexPattern) =>
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
            string leadingSubstring = TextSegment.SubstringBefore(StartIndex);
            if (leadingSubstring.Length == 0)
                return false;

            TextElementEnumerator charactersEnumerator = StringInfo.GetTextElementEnumerator(leadingSubstring);
            while (charactersEnumerator.MoveNext())
            {
                if (!IsLetter(charactersEnumerator.Current.ToString()))
                    return false;
            }
            return true;
        }

        public bool HasLetterInTrailingSubstring()
        {
            string trailingSubstring = TextSegment.SubstringAfter(EndIndex);
            if (trailingSubstring.Length == 0)
                return false;
            TextElementEnumerator charactersEnumerator = StringInfo.GetTextElementEnumerator(trailingSubstring);
            while (charactersEnumerator.MoveNext())
            {
                if (!IsLetter(charactersEnumerator.Current.ToString()))
                    return false;
            }
            return true;
        }

        public bool HasLeadingLatinLetter()
        {
            return PreviousCharacter != null && IsLatinScript(PreviousCharacter);
        }

        public bool HasTrailingLatinLetter()
        {
            return NextCharacter != null && IsLatinScript(NextCharacter);
        }

        public bool HasQuoteIntroducerInLeadingSubstring()
        {
            return LeadingSubstringMatches(QuoteIntroducerPattern);
        }

        public static bool HasUnicodeProperty(string characterString, string attribute)
        {
            if (characterString.Length == 1)
            {
                return UnicodeInfo.GetName(characterString[0]).Contains(attribute);
            }
            else if (char.IsSurrogatePair(characterString[0], characterString[1]))
            {
                //Get true unicode value
                int combinedCharacterValue =
                    (((int)characterString[0] - 0xD800) * 0x400) + ((int)characterString[1] - 0xDC00) + 0x10000;
                return UnicodeInfo.GetName(combinedCharacterValue).Contains(attribute);
            }
            return false;
        }

        private bool IsLatinScript(string characterString)
        {
            return HasUnicodeProperty(characterString, "LATIN");
        }

        private bool IsLetter(string characterString)
        {
            return HasUnicodeProperty(characterString, "LETTER");
        }
    }
}
