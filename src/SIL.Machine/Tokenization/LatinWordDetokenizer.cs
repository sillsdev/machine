using System.Collections.Generic;
using System.Globalization;
using SIL.Extensions;

namespace SIL.Machine.Tokenization
{
    public class LatinWordDetokenizer : StringDetokenizer
    {
        private enum QuoteType
        {
            DoubleQuotation,
            SingleQuotation,
            DoubleAngle,
            SingleAngle
        }

        private static readonly Dictionary<char, QuoteType> s_quotationMarks = new Dictionary<char, QuoteType>
        {
            { '"', QuoteType.DoubleQuotation },
            { '“', QuoteType.DoubleQuotation },
            { '”', QuoteType.DoubleQuotation },
            { '„', QuoteType.DoubleQuotation },
            { '‟', QuoteType.DoubleQuotation },
            { '\'', QuoteType.SingleQuotation },
            { '‘', QuoteType.SingleQuotation },
            { '’', QuoteType.SingleQuotation },
            { '‚', QuoteType.SingleQuotation },
            { '‛', QuoteType.SingleQuotation },
            { '«', QuoteType.DoubleAngle },
            { '»', QuoteType.DoubleAngle },
            { '‹', QuoteType.SingleAngle },
            { '›', QuoteType.SingleAngle }
        };

        protected override object CreateContext()
        {
            return new Stack<char>();
        }

        protected override DetokenizeOperation GetOperation(object context, string token)
        {
            var quotes = (Stack<char>)context;
            char c = token[0];
            if (
                CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.CurrencySymbol
                || c.IsOneOf('(', '[', '{', '¿', '¡', '<')
            )
            {
                return DetokenizeOperation.MergeRight;
            }
            else if (s_quotationMarks.ContainsKey(c))
            {
                if (quotes.Count == 0 || s_quotationMarks[c] != s_quotationMarks[quotes.Peek()])
                {
                    quotes.Push(c);
                    return DetokenizeOperation.MergeRight;
                }
                else
                {
                    quotes.Pop();
                    return DetokenizeOperation.MergeLeft;
                }
            }
            else if (c == '/' || c == '\\')
                return DetokenizeOperation.MergeBoth;
            else if (char.IsPunctuation(c) || c == '>')
                return DetokenizeOperation.MergeLeft;

            return DetokenizeOperation.NoOperation;
        }
    }
}
