using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SIL.PlatformUtilities;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	internal static class CorporaHelpers
	{
		internal static IEnumerable<(string Id, string FileName)> GetFiles(IEnumerable<string> filePatterns)
		{
			string[] filePatternArray = filePatterns.ToArray();
			if (filePatternArray.Length == 1 && File.Exists(filePatternArray[0]))
			{
				yield return ("*all*", filePatternArray[0]);
			}
			else
			{
				foreach (string filePattern in filePatternArray)
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

		internal static string GetScriptureTextSortKey(string id)
		{
			return Canon.BookIdToNumber(id).ToString("000");
		}

		internal static string GetUsxId(string fileName)
		{
			string name = Path.GetFileNameWithoutExtension(fileName);
			if (name.Length == 3)
				return name;
			return name.Substring(3, 3);
		}

		internal static string MergeVerseRanges(string verse1, string verse2)
		{
			var sb = new StringBuilder();
			int startVerseNum = -1;
			int prevVerseNum = -1;
			foreach (int verseNum in GetVerseNums(verse1).Union(GetVerseNums(verse2)).OrderBy(vn => vn))
			{
				if (prevVerseNum == -1)
				{
					startVerseNum = verseNum;
				}
				else if (prevVerseNum != verseNum - 1)
				{
					AppendVerseRange(sb, startVerseNum, prevVerseNum);
					startVerseNum = verseNum;
				}
				prevVerseNum = verseNum;
			}
			AppendVerseRange(sb, startVerseNum, prevVerseNum);

			return sb.ToString();
		}

		private static void AppendVerseRange(StringBuilder sb, int startVerseNum, int endVerseNum)
		{
			if (sb.Length > 0)
				sb.Append(VerseRef.verseSequenceIndicator);
			sb.Append(startVerseNum);
			if (endVerseNum != startVerseNum)
			{
				sb.Append(VerseRef.verseRangeSeparator);
				sb.Append(endVerseNum);
			}
		}

		private static IEnumerable<int> GetVerseNums(string verse)
		{
			string[] source = verse.Split(VerseRef.verseSequenceIndicators, StringSplitOptions.None);
			foreach (string[] pieces in source.Select(part => part.Split(VerseRef.verseRangeSeparators,
				StringSplitOptions.None)))
			{
				int startVerseNum = int.Parse(pieces[0], CultureInfo.InvariantCulture);
				yield return startVerseNum;
				if (pieces.Length <= 1)
					continue;

				int endVerseNum = int.Parse(pieces[1], CultureInfo.InvariantCulture);
				for (int verseNum = startVerseNum + 1; verseNum < endVerseNum; verseNum++)
					yield return verseNum;

				yield return endVerseNum;
			}
		}
	}
}
