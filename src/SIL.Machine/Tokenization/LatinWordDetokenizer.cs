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

		private readonly static Dictionary<char, QuoteType> QuotationMarks = new Dictionary<char, QuoteType>
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

		protected override DetokenizeOperation GetOperation(object ctxt, string token)
		{
			var quotes = (Stack<char>)ctxt;
			if (token.Length == 1)
			{
				char c = token[0];
				if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.CurrencySymbol
					|| c.IsOneOf('(', '[', '{', '¿', '¡', '<'))
				{
					return DetokenizeOperation.MergeRight;
				}
				else if (QuotationMarks.ContainsKey(c))
				{
					if (quotes.Count == 0 || QuotationMarks[c] != QuotationMarks[quotes.Peek()])
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
				{
					return DetokenizeOperation.MergeBoth;
				}
				else if (char.IsPunctuation(c) || c == '>')
				{
					return DetokenizeOperation.MergeLeft;
				}
			}

			return DetokenizeOperation.NoOperation;
		}
	}
}
