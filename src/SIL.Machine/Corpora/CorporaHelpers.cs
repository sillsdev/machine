using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SIL.PlatformUtilities;

namespace SIL.Machine.Corpora
{
	internal static class CorporaHelpers
	{
		internal static IEnumerable<(string Id, string FileName)> GetFiles(IEnumerable<string> filePatterns)
		{
			foreach (string filePattern in filePatterns)
			{
				string path = filePattern;
				string searchPattern = "*";
				if (!filePattern.EndsWith(Path.PathSeparator.ToString()) && !Directory.Exists(filePattern))
				{
					path = Path.GetDirectoryName(filePattern);
					searchPattern = Path.GetFileName(filePattern);
				}

				if (path == "")
					path = ".";

				string convertedMask = "^" + Regex.Escape(Path.GetFileNameWithoutExtension(searchPattern))
					.Replace("\\*", "(.*)").Replace("\\?", "(.)") + "$";
				var maskRegex = new Regex(convertedMask,
					Platform.IsWindows ? RegexOptions.IgnoreCase : RegexOptions.None);

				foreach (string fileName in Directory.EnumerateFiles(path, searchPattern))
				{
					string id = Path.GetFileNameWithoutExtension(fileName);
					Match match = maskRegex.Match(id);
					if (match.Success)
					{
						var sb = new StringBuilder();
						for (int i = 1; i < match.Groups.Count; i++)
						{
							if (!match.Groups[i].Success)
								continue;

							if (sb.Length > 0)
								sb.Append("-");
							sb.Append(match.Groups[i].Value);
						}
						if (sb.Length > 0)
							id = sb.ToString();
					}
					yield return (id, fileName);
				}
			}
		}
	}
}
