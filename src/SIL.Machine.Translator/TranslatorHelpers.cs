using System;
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
		public static bool ValidateCorpusFormatOption(string value)
		{
			return string.IsNullOrEmpty(value) || value.ToLowerInvariant().IsOneOf("dbl", "usx", "text", "pt");
		}

		public static bool ValidateWordTokenizerOption(string value, bool supportsNullTokenizer = false)
		{
			var types = new HashSet<string> { "latin", "whitespace", "zwsp" };
			if (supportsNullTokenizer)
				types.Add("none");
			return string.IsNullOrEmpty(value) || types.Contains(value.ToLowerInvariant());
		}

		public static ITextCorpus CreateTextCorpus(ITokenizer<string, int, string> wordTokenizer, string type,
			string path)
		{
			switch (type.ToLowerInvariant())
			{
				case "dbl":
					return new DblBundleTextCorpus(wordTokenizer, path);

				case "usx":
					return new UsxFileTextCorpus(wordTokenizer, path);

				case "pt":
					return new ParatextTextCorpus(wordTokenizer, path);

				case "text":
					return new TextFileTextCorpus(wordTokenizer, path);
			}

			throw new ArgumentException("An invalid text corpus type was specified.", nameof(type));
		}

		public static ITextAlignmentCorpus CreateAlignmentsCorpus(string type, string path)
		{
			switch (type.ToLowerInvariant())
			{
				case "text":
					return new TextFileTextAlignmentCorpus(path);
			}

			throw new ArgumentException("An invalid alignment corpus type was specified.", nameof(type));
		}

		public static IRangeTokenizer<string, int, string> CreateWordTokenizer(string type)
		{
			switch (type.ToLowerInvariant())
			{
				case "latin":
					return new LatinWordTokenizer();

				case "none":
					return new NullTokenizer();

				case "zwsp":
					return new ZwspWordTokenizer();

				case "whitespace":
					return new WhitespaceTokenizer();
			}

			throw new ArgumentException("An invalid tokenizer type was specified.", nameof(type));
		}

		public static IDetokenizer<string, string> CreateWordDetokenizer(string type)
		{
			switch (type.ToLowerInvariant())
			{
				case "latin":
					return new LatinWordDetokenizer();

				case "zwsp":
					return new ZwspWordDetokenizer();

				case "whitespace":
					return new WhitespaceDetokenizer();
			}

			throw new ArgumentException("An invalid tokenizer type was specified.", nameof(type));
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

		public static string GetTranslationModelConfigFileName(string path)
		{
			if (File.Exists(path))
				return path;
			else if (Directory.Exists(path) || TranslatorHelpers.IsDirectoryPath(path))
				return Path.Combine(path, "smt.cfg");
			else
				return path;
		}
	}
}
