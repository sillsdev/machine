using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SIL.Machine.Corpora
{
	public class UsxVerse
	{
		public UsxVerse(string chapter, string verse, bool sentenceStart, IEnumerable<XNode> nodes)
		{
			Chapter = chapter;
			Verse = verse;
			SentenceStart = sentenceStart;
			Nodes = nodes.ToArray();

			XElement prevParent = null;
			var sb = new StringBuilder();
			foreach (XNode node in Nodes)
			{
				XElement parent = node.Parent;
				while (parent != null && parent.Name != "para")
					parent = parent.Parent;

				if (parent != prevParent && sb.Length > 0)
					sb.Append(" ");

				sb.Append(node);
				prevParent = parent;
			}
			Text = sb.ToString().Trim();
		}

		public string Chapter { get; }
		public string Verse { get; }
		public bool SentenceStart { get; }
		public IReadOnlyList<XNode> Nodes { get; }
		public string Text { get; }
	}
}
