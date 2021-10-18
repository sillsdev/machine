using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.Utils;

namespace SIL.Machine.Corpora
{
	public class UsxVerse
	{
		public UsxVerse(string chapter, string verse, bool isSentenceStart, IEnumerable<UsxToken> tokens)
		{
			Chapter = chapter;
			Verse = verse;
			IsSentenceStart = isSentenceStart;
			Tokens = tokens.ToArray();

			UsxToken prevToken = null;
			var sb = new StringBuilder();
			bool endsWithSpace = false;
			foreach (UsxToken token in Tokens)
			{
				if (token.Element != null && token.Element.Name == "figure" && !endsWithSpace)
					sb.Append(" ");

				if (token.Text.Length == 0 || token.Text.IsWhiteSpace())
					continue;

				if (prevToken != null && token.ParaElement != prevToken.ParaElement && sb.Length > 0 && !endsWithSpace)
					sb.Append(" ");

				sb.Append(token);
				endsWithSpace = token.Text.EndsWith(" ");
				prevToken = token;
			}
			Text = sb.ToString().Trim();
		}

		public string Chapter { get; }
		public string Verse { get; }
		public bool IsSentenceStart { get; }
		public IReadOnlyList<UsxToken> Tokens { get; }
		public string Text { get; }
	}
}
