using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

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
			foreach (UsxToken token in Tokens)
			{
				if (token.ParaElement != prevParaElem && sb.Length > 0)
					sb.Append(" ");

				sb.Append(token);
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
