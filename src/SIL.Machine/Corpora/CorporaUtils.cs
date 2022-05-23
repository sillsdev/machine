using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SIL.PlatformUtilities;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public static class CorporaUtils
    {
        public static ISet<int> GetSplitIndices(
            int corpusSize,
            double? percent = null,
            int? size = null,
            int? seed = null
        )
        {
            if (percent == null && size == null)
                percent = 0.1;

            int splitSize;
            if (percent != null)
            {
                splitSize = (int)(percent * corpusSize);
                if (size != null)
                    splitSize = Math.Min(splitSize, size.Value);
            }
            else
            {
                splitSize = size.Value;
            }

            var r = seed != null ? new Random(seed.Value) : new Random();
            return new HashSet<int>(Enumerable.Range(0, corpusSize).OrderBy(i => r.Next()).Take(splitSize));
        }

        public static string MergeVerseRanges(string verse1, string verse2)
        {
            var sb = new StringBuilder();
            (int, string) startVerseNum = (-1, null);
            (int, string) prevVerseNum = (-1, null);
            foreach ((int, string) verseNum in GetVerseNums(verse1).Union(GetVerseNums(verse2)).OrderBy(vn => vn.Item1))
            {
                if (prevVerseNum.Item1 == -1)
                {
                    startVerseNum = verseNum;
                }
                else if (prevVerseNum.Item1 != verseNum.Item1 - 1)
                {
                    AppendVerseRange(sb, startVerseNum.Item2, prevVerseNum.Item2);
                    startVerseNum = verseNum;
                }
                prevVerseNum = verseNum;
            }
            AppendVerseRange(sb, startVerseNum.Item2, prevVerseNum.Item2);

            return sb.ToString();
        }

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

                    string convertedMask =
                        "^"
                        + Regex
                            .Escape(Path.GetFileNameWithoutExtension(searchPattern))
                            .Replace("\\*", "(.*)")
                            .Replace("\\?", "(.)")
                        + "$";
                    var maskRegex = new Regex(
                        convertedMask,
                        Platform.IsWindows ? RegexOptions.IgnoreCase : RegexOptions.None
                    );

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

        private static void AppendVerseRange(StringBuilder sb, string startVerseNum, string endVerseNum)
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

        private static IEnumerable<(int, string)> GetVerseNums(string verse)
        {
            string[] source = verse.Split(VerseRef.verseSequenceIndicators, StringSplitOptions.None);
            foreach (
                string[] pieces in source.Select(
                    part => part.Split(VerseRef.verseRangeSeparators, StringSplitOptions.None)
                )
            )
            {
                int startVerseNum = GetVerseNum(pieces[0]);
                yield return (startVerseNum, pieces[0]);
                if (pieces.Length <= 1)
                    continue;

                int endVerseNum = GetVerseNum(pieces[1]);
                for (int verseNum = startVerseNum + 1; verseNum < endVerseNum; verseNum++)
                    yield return (verseNum, verseNum.ToString());

                yield return (endVerseNum, pieces[1]);
            }
        }

        private static int GetVerseNum(string verseStr)
        {
            int vNum = 0;
            for (int i = 0; i < verseStr.Length; i++)
            {
                char ch = verseStr[i];
                if (!char.IsDigit(ch))
                    break;

                vNum = (int)(vNum * 10 + char.GetNumericValue(ch));
            }
            return vNum;
        }
    }
}
