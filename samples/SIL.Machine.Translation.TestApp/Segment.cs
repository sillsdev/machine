using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.Machine.Translation.TestApp
{
	public class Segment
	{
		public static readonly Regex TokenizeRegex = new Regex(@"[\p{P}]|(\w+([.,\-’']\w+)*)");

		public Segment()
		{
			Text = string.Empty;
		}

		public string Text { get; set; }

		public int StartIndex { get; set; }

		public bool IsApproved { get; set; }

		public IEnumerable<string> Words
		{
			get { return TokenizeRegex.Matches(Text).Cast<Match>().Select(m => m.Value); }
		}

		public override string ToString()
		{
			return Text;
		}
	}
}
