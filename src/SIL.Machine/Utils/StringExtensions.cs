using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.Machine.Utils
{
    public static class StringExtensions
    {
        private static readonly HashSet<char> SentenceTerminals = new HashSet<char>
        {
            '.',
            '!',
            '?',
            '\u203C',
            '\u203D',
            '\u2047',
            '\u2048',
            '\u2049',
            '\u3002',
            '\uFE52',
            '\uFE57',
            '\uFF01',
            '\uFF0E',
            '\uFF1F',
            '\uFF61'
        };

        private static readonly HashSet<char> QuotationMarks = new HashSet<char>
        {
            '"',
            '“',
            '”',
            '„',
            '‟',
            '\'',
            '‘',
            '’',
            '‚',
            '‛',
            '«',
            '»',
            '‹',
            '›'
        };

        private static readonly HashSet<char> DelayedSentenceStart = new HashSet<char>(QuotationMarks)
        {
            '(',
            '[',
            '<',
            '{'
        };

        private static readonly HashSet<char> DelayedSentenceEnd = new HashSet<char>(QuotationMarks)
        {
            ')',
            ']',
            '>',
            '}'
        };

        public static bool IsSentenceTerminal(this char c)
        {
            return SentenceTerminals.Contains(c);
        }

        public static bool IsSentenceTerminal(this string str)
        {
            return str.Length > 0 && str.All(c => SentenceTerminals.Contains(c));
        }

        public static bool IsDelayedSentenceStart(this char c)
        {
            return DelayedSentenceStart.Contains(c);
        }

        public static bool IsDelayedSentenceStart(this string str)
        {
            return str.Length > 0 && str.All(c => DelayedSentenceStart.Contains(c));
        }

        public static bool IsDelayedSentenceEnd(this char c)
        {
            return DelayedSentenceEnd.Contains(c);
        }

        public static bool IsDelayedSentenceEnd(this string str)
        {
            return str.Length > 0 && str.All(c => DelayedSentenceEnd.Contains(c));
        }

        public static bool HasSentenceEnding(this string str)
        {
            str = str.TrimEnd();
            for (int i = str.Length - 1; i >= 0; i--)
            {
                if (str[i].IsSentenceTerminal())
                    return true;
                if (!str[i].IsDelayedSentenceEnd())
                    return false;
            }
            return false;
        }

        public static bool IsTitleCase(this string str)
        {
            return str.Length > 0
                && char.IsUpper(str, 0)
                && Enumerable.Range(1, str.Length - 1).All(i => char.IsLower(str, i));
        }

        public static bool IsLower(this string str)
        {
            return str.Length > 0 && str.All(char.IsLower);
        }

        public static string ToTitleCase(this string str)
        {
            if (str.Length == 0)
                return str;

            var sb = new StringBuilder();
            sb.Append(str.Substring(0, 1).ToUpperInvariant());
            if (str.Length > 1)
                sb.Append(str.Substring(1, str.Length - 1).ToLowerInvariant());
            return sb.ToString();
        }

        public static bool IsWhiteSpace(this string str)
        {
            return str.Length > 0 && str.All(char.IsWhiteSpace);
        }

        public static IReadOnlyList<string> ToSentenceCase(
            this IReadOnlyList<string> segment,
            bool sentenceStart = true
        )
        {
            var result = new string[segment.Count];
            for (int i = 0; i < segment.Count; i++)
            {
                string token = segment[i];
                if (sentenceStart && token.IsLower())
                    token = token.ToTitleCase();
                result[i] = token;
                if (token.IsSentenceTerminal())
                    sentenceStart = true;
                else if (!token.IsDelayedSentenceStart())
                    sentenceStart = false;
            }
            return result;
        }
    }
}
