using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
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

			XElement prevParaElem = null;
			var sb = new StringBuilder();
			bool endsWithSpace = false;
			foreach (UsxToken token in Tokens)
			{
				if (token.Text.Length == 0 || token.Text.IsWhiteSpace())
					continue;

				if (token.ParaElement != prevParaElem && sb.Length > 0 && !endsWithSpace)
					sb.Append(" ");

				sb.Append(token);
				endsWithSpace = token.Text.EndsWith(" ");
				prevParaElem = token.ParaElement;
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
