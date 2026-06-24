using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.Machine.Corpora
{
    // This class is used by SegmentBoundaryAdjuster when it is dealing with tokenized text.
    public class TokenRejoiner
    {
        private static readonly HashSet<string> NoTrailingSpaceCharacters = new HashSet<string>
        {
            "(",
            "[",
            "{",
            "\u00ab",
            "\u2039",
            "\u201c",
            "\u2018",
            "\u201a",
            "\u201e",
            "<<",
            "<",
        };
        private static readonly HashSet<string> NoLeadingSpaceCharacters = new HashSet<string>
        {
            ",",
            ";",
            ":",
            ".",
            "!",
            "?",
            ")",
            "]",
            "}",
            "\u201d",
            "\u2019",
            "\u00bb",
            "\u203a",
            ">",
            ">>",
        };

        private string _joinedText = "";
        private int _numTokens = 0;

        public static string JoinTokens(IEnumerable<string> tokens)
        {
            var rejoiner = new TokenRejoiner();
            foreach (string token in tokens)
                rejoiner.AddTokenToJoinedText(token);
            if (rejoiner._joinedText.Length > 0 && !EndsWithNoTrailingSpaceChar(rejoiner._joinedText))
                rejoiner._joinedText += " ";
            return rejoiner._joinedText;
        }

        public string AddTokenToJoinedText(string token)
        {
            if (_numTokens > 0)
            {
                if (!NoLeadingSpaceCharacters.Contains(token) && !EndsWithNoTrailingSpaceChar(_joinedText))
                    _joinedText += " ";
            }
            _joinedText += token;
            _numTokens++;
            return _joinedText;
        }

        private static bool EndsWithNoTrailingSpaceChar(string text)
        {
            if (text.Length == 0)
                return false;
            if (NoTrailingSpaceCharacters.Contains(text[text.Length - 1].ToString()))
                return true;
            if (text.Length >= 2 && NoTrailingSpaceCharacters.Contains(text.Substring(text.Length - 2)))
                return true;
            return false;
        }
    }

    public class SegmentBoundaryAdjuster
    {
        // Guillemets are not included in this list because they can be used as quote continuers
        // in some contexts (e.g. Spanish)
        private static readonly HashSet<char> ProhibitedSegmentStartingCharacters = new HashSet<char>
        {
            ' ',
            ',',
            ';',
            ':',
            '.',
            '!',
            '?',
            ')',
            ']',
            '}',
            '\u201d',
            '\u2019',
        };
        private static readonly HashSet<char> ProhibitedSegmentEndingCharacters = new HashSet<char>
        {
            '(',
            '[',
            '{',
            '\u00ab',
            '\u2039',
            '\u201c',
            '\u2018',
        };
        private static readonly Regex PunctuationAndSentenceStartingPattern = new Regex(
            @".*([^\w\s]\s*)(\p{Lu}\w+(\s+\w+)?(\s+\w+)?\s*)$",
            RegexOptions.Compiled
        );
        private static readonly Regex WordsAndSentenceEndingPattern = new Regex(
            @"^(\p{Ll}\w+(\s+\w+)?(\s+\w+)?)([.,;:!?\)\]\u201d\u2019]\s*[\u201d\u2019]*\s*)",
            RegexOptions.Compiled
        );

        public List<string> AdjustSegmentBoundaries(List<string> verses)
        {
            for (int i = 0; i < verses.Count - 1; i++)
                (verses[i], verses[i + 1]) = AdjustSegmentPairBoundary(verses[i], verses[i + 1]);
            return verses;
        }

        public (string Segment, string NextSegment) AdjustSegmentPairBoundary(string segment, string nextSegment)
        {
            while (nextSegment.Length > 0 && ProhibitedSegmentStartingCharacters.Contains(nextSegment[0]))
            {
                segment += nextSegment[0];
                nextSegment = nextSegment.Substring(1);
            }
            while (segment.Length > 0 && ProhibitedSegmentEndingCharacters.Contains(segment[segment.Length - 1]))
            {
                nextSegment = segment[segment.Length - 1] + nextSegment;
                segment = segment.Substring(0, segment.Length - 1);
            }
            if (SegmentEndsWithStartOfSentence(segment))
                (segment, nextSegment) = AdjustForMissedSentenceStart(segment, nextSegment);
            if (SegmentStartsWithEndOfSentence(nextSegment))
                (segment, nextSegment) = AdjustForLateSentenceEnd(segment, nextSegment);
            return (segment, nextSegment);
        }

        private bool SegmentEndsWithStartOfSentence(string segment)
        {
            return PunctuationAndSentenceStartingPattern.IsMatch(segment);
        }

        private (string, string) AdjustForMissedSentenceStart(string segment, string nextSegment)
        {
            Match match = PunctuationAndSentenceStartingPattern.Match(segment);
            if (match.Success)
            {
                string capitalizedWord = match.Groups[2].Value;
                segment = segment.Substring(0, match.Groups[1].Index + match.Groups[1].Length);
                nextSegment =
                    capitalizedWord
                    + (capitalizedWord[capitalizedWord.Length - 1] == ' ' ? "" : " ")
                    + nextSegment;
            }
            return (segment, nextSegment);
        }

        private bool SegmentStartsWithEndOfSentence(string segment)
        {
            return WordsAndSentenceEndingPattern.IsMatch(segment);
        }

        private (string, string) AdjustForLateSentenceEnd(string segment, string nextSegment)
        {
            Match match = WordsAndSentenceEndingPattern.Match(nextSegment);
            if (match.Success)
            {
                string words = match.Groups[1].Value;
                string punctuation = match.Groups[4].Value;
                segment = segment + words + punctuation;
                nextSegment = nextSegment.Substring(match.Index + match.Length);
            }
            return (segment, nextSegment);
        }

        public int AdjustTokenizedSegmentPairBoundaries(int segmentBoundary, IReadOnlyList<string> tokens)
        {
            string segmentText = TokenRejoiner.JoinTokens(tokens.Take(segmentBoundary));
            string nextSegmentText = TokenRejoiner.JoinTokens(tokens.Skip(segmentBoundary));
            string adjustedSegmentText = AdjustSegmentPairBoundary(segmentText, nextSegmentText).Segment.Trim();
            return FindBestBoundaryFromSegmentLength(tokens, adjustedSegmentText.Length);
        }

        private int FindBestBoundaryFromSegmentLength(IReadOnlyList<string> tokens, int targetSegmentLength)
        {
            var tokenRejoiner = new TokenRejoiner();
            for (int index = 0; index < tokens.Count; index++)
            {
                int accumulatedLength = tokenRejoiner.AddTokenToJoinedText(tokens[index]).Length;
                if (accumulatedLength >= targetSegmentLength)
                {
                    int errorWithCurrentBoundary = accumulatedLength - targetSegmentLength;
                    int errorWithPreviousBoundary =
                        targetSegmentLength - (accumulatedLength - tokens[index].Length);
                    if (errorWithCurrentBoundary < errorWithPreviousBoundary)
                        return index + 1;
                    else
                        return index;
                }
            }
            return tokens.Count;
        }
    }
}
