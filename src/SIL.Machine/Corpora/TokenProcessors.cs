using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.Machine.Corpora
{
	public delegate IReadOnlyList<string> TokenProcessor(IReadOnlyList<string> tokens);

	public static class TokenProcessors
	{
		public static IReadOnlyList<string> NoOp(this IReadOnlyList<string> tokens)
		{
			return tokens;
		}

		public static IReadOnlyList<string> EscapeSpaces(this IReadOnlyList<string> tokens)
		{
			return tokens.Select(t => t.Length > 0 && t.All(char.IsWhiteSpace) ? "<space>" : t).ToArray();
		}

		public static IReadOnlyList<string> UnescapeSpaces(this IReadOnlyList<string> tokens)
		{
			return tokens.Select(t => t == "<space>" ? " " : t).ToArray();
		}

		public static IReadOnlyList<string> Lowercase(this IReadOnlyList<string> tokens)
		{
			return tokens.Select(t => t.ToLowerInvariant()).ToArray();
		}

		public static IReadOnlyList<string> Uppercase(this IReadOnlyList<string> tokens)
		{
			return tokens.Select(t => t.ToUpperInvariant()).ToArray();
		}

		public static IReadOnlyList<string> Normalize(this IReadOnlyList<string> tokens,
			NormalizationForm normalizationForm = NormalizationForm.FormC)
		{
			return tokens.Select(t => t.Normalize(normalizationForm)).ToArray();
		}

		public static IReadOnlyList<string> NfcNormalize(this IReadOnlyList<string> tokens)
		{
			return tokens.Normalize();
		}

		public static IReadOnlyList<string> NfdNormalize(this IReadOnlyList<string> tokens)
		{
			return tokens.Normalize(NormalizationForm.FormD);
		}

		public static IReadOnlyList<string> NfkcNormalize(this IReadOnlyList<string> tokens)
		{
			return tokens.Normalize(NormalizationForm.FormKC);
		}

		public static IReadOnlyList<string> NfkdNormalize(this IReadOnlyList<string> tokens)
		{
			return tokens.Normalize(NormalizationForm.FormKD);
		}
	}
}
