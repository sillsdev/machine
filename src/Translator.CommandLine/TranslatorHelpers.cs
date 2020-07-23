﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Translation
{
	internal static class TranslatorHelpers
	{
		public static string GetEngineConfigFileName(string path)
		{
			if (File.Exists(path))
				return path;
			else if (Directory.Exists(path) || IsDirectoryPath(path))
				return Path.Combine(path, "smt.cfg");
			else
				return path;
		}

		public static bool ValidateTextCorpusOption(string value, out string type, out string path)
		{
			if (ValidateCorpusOption(value, out type, out path))
				return string.IsNullOrEmpty(type) || type.IsOneOf("dbl", "usx", "text", "pt");
			return false;
		}

		public static bool ValidateAlignmentsOption(string value, out string type, out string path)
		{
			if (string.IsNullOrEmpty(value))
			{
				type = null;
				path = null;
				return true;
			}

			if (ValidateCorpusOption(value, out type, out path))
				return string.IsNullOrEmpty(type) || type == "text";
			return false;
		}

		public static bool ValidateWordTokenizerOption(string value, bool supportsNullTokenizer)
		{
			var types = new HashSet<string> { "latin", "whitespace" };
			if (supportsNullTokenizer)
				types.Add("null");
			return string.IsNullOrEmpty(value) || types.Contains(value);
		}

		public static ITextCorpus CreateTextCorpus(StringTokenizer wordTokenizer, string type, string path)
		{
			switch (type)
			{
				case "dbl":
					return new DblBundleTextCorpus(wordTokenizer, path);

				case "usx":
					return new UsxFileTextCorpus(wordTokenizer, path);

				case "pt":
					return new ParatextTextCorpus(wordTokenizer, path);

				case "text":
				default:
					if (File.Exists(path))
						return TextFileText.CreateSingleFileCorpus(wordTokenizer, path);
					return new TextFileTextCorpus(wordTokenizer, path);
			}
		}

		public static ITextAlignmentCorpus CreateAlignmentsCorpus(string type, string path)
		{
			switch (type)
			{
				case "text":
				default:
					return new TextFileTextAlignmentCorpus(path);
			}
		}

		public static StringTokenizer CreateWordTokenizer(string type)
		{
			switch (type)
			{
				case "latin":
					return new LatinWordTokenizer();

				case "null":
					return new NullTokenizer();

				case "whitespace":
				default:
					return new WhitespaceTokenizer();
			}
		}

		public static StringDetokenizer CreateWordDetokenizer(string type)
		{
			switch (type)
			{
				case "latin":
					return new LatinWordDetokenizer();

				case "whitespace":
				default:
					return new WhitespaceDetokenizer();
			}
		}

		public static ISet<string> GetTexts(IEnumerable<string> values)
		{
			var ids = new HashSet<string>();
			foreach (string value in values)
			{
				foreach (string id in value.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries))
				{
					if (id == "*NT*")
						ids.UnionWith(Canon.AllBookIds.Where(i => Canon.IsBookNT(i)));
					else if (id == "*OT*")
						ids.UnionWith(Canon.AllBookIds.Where(i => Canon.IsBookOT(i)));
					else
						ids.Add(id);
				}
			}
			return ids;
		}

		public static bool IsDirectoryPath(string path)
		{
			if (Directory.Exists(path))
				return true;
			string separator1 = Path.DirectorySeparatorChar.ToString();
			string separator2 = Path.AltDirectorySeparatorChar.ToString();
			path = path.TrimEnd();
			return path.EndsWith(separator1) || path.EndsWith(separator2);
		}

		private static bool ValidateCorpusOption(string value, out string type, out string path)
		{
			type = null;

			int index = value.IndexOf(",", StringComparison.Ordinal);
			if (index == -1)
			{
				path = value;
			}
			else
			{
				type = value.Substring(0, index).ToLowerInvariant();
				path = value.Substring(index + 1);
			}
			return path != "";
		}
	}
}