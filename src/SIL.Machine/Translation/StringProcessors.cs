using System.Linq;

namespace SIL.Machine.Translation
{
	public static class StringProcessors
	{
		public static string Lowercase(string str)
		{
			return str.ToLowerInvariant();
		}

		public static string EscapeSpaces(string str)
		{
			if (str.Any(char.IsWhiteSpace))
				return "<space>";
			return str;
		}

		public static string UnescapeSpaces(string str)
		{
			if (str == "<space>")
				return " ";
			return str;
		}

		public static string Null(string str)
		{
			return str;
		}
	}
}
