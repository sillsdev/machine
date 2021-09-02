using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SIL.Extensions;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;
using SIL.Scripture;

namespace SIL.Machine
{
	internal static class ToolHelpers
	{
		public const string Hmm = "hmm";
		public const string Ibm1 = "ibm1";
		public const string Ibm2 = "ibm2";
		public const string FastAlign = "fast_align";

		public const string Och = "och";
		public const string Union = "union";
		public const string Intersection = "intersection";
		public const string Grow = "grow";
		public const string GrowDiag = "grow-diag";
		public const string GrowDiagFinal = "grow-diag-final";
		public const string GrowDiagFinalAnd = "grow-diag-final-and";
		public const string None = "none";

		public static bool ValidateCorpusFormatOption(string value)
		{
			return string.IsNullOrEmpty(value)
				|| value.ToLowerInvariant().IsOneOf("dbl", "usx", "text", "pt", "pt_m");
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

				case "pt_m":
					return new ParatextTextCorpus(wordTokenizer, path, includeMarkers: true);
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
			else if (Directory.Exists(path) || IsDirectoryPath(path))
				return Path.Combine(path, "smt.cfg");
			else
				return path;
		}

		public static bool ValidateTranslationModelTypeOption(string value)
		{
			var validTypes = new HashSet<string> { Hmm, Ibm1, Ibm2, FastAlign };
			return string.IsNullOrEmpty(value) || validTypes.Contains(value);
		}

		public static ThotWordAlignmentModelType GetThotWordAlignmentModelType(string modelType)
		{
			switch (modelType)
			{
				default:
				case Hmm:
					return ThotWordAlignmentModelType.Hmm;
				case Ibm1:
					return ThotWordAlignmentModelType.Ibm1;
				case Ibm2:
					return ThotWordAlignmentModelType.Ibm2;
				case FastAlign:
					return ThotWordAlignmentModelType.FastAlign;
			}
		}

		public static ITrainer CreateTranslationModelTrainer(string modelType,
			string modelConfigFileName, ParallelTextCorpus corpus, int maxSize, ITokenProcessor processor)
		{
			ThotWordAlignmentModelType wordAlignmentModelType = GetThotWordAlignmentModelType(modelType);

			string modelDir = Path.GetDirectoryName(modelConfigFileName);
			if (!Directory.Exists(modelDir))
				Directory.CreateDirectory(modelDir);

			if (!File.Exists(modelConfigFileName))
				CreateConfigFile(wordAlignmentModelType, modelConfigFileName);

			return new ThotSmtModelTrainer(wordAlignmentModelType, modelConfigFileName, processor, processor, corpus,
				maxSize);
		}

		public static bool ValidateSymmetrizationHeuristicOption(string value, bool noneAllowed = true)
		{
			var validHeuristics = new HashSet<string>
			{
				Och,
				Union,
				Intersection,
				Grow,
				GrowDiag,
				GrowDiagFinal,
				GrowDiagFinalAnd
			};
			if (noneAllowed)
				validHeuristics.Add(None);
			return string.IsNullOrEmpty(value) || validHeuristics.Contains(value.ToLowerInvariant());
		}

		public static SymmetrizationHeuristic GetSymmetrizationHeuristic(string value)
		{
			return value switch
			{
				None => SymmetrizationHeuristic.None,
				Union => SymmetrizationHeuristic.Union,
				Intersection => SymmetrizationHeuristic.Intersection,
				Grow => SymmetrizationHeuristic.Grow,
				GrowDiag => SymmetrizationHeuristic.GrowDiag,
				GrowDiagFinal => SymmetrizationHeuristic.GrowDiagFinal,
				GrowDiagFinalAnd => SymmetrizationHeuristic.GrowDiagFinalAnd,
				_ => SymmetrizationHeuristic.Och,
			};
		}

		public static StreamWriter CreateStreamWriter(string fileName)
		{
			var utf8Encoding = new UTF8Encoding(false);
			return new StreamWriter(fileName, false, utf8Encoding) { NewLine = "\n" };
		}

		private static void CreateConfigFile(ThotWordAlignmentModelType wordAlignmentModelType,
			string modelConfigFileName)
		{
			string defaultConfigFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data",
				"default-smt.cfg");
			string text = File.ReadAllText(defaultConfigFileName);
			int emIters = 5;
			if (wordAlignmentModelType == ThotWordAlignmentModelType.FastAlign)
				emIters = 4;
			text = text.Replace("{em_iters}", $"{emIters}");
			File.WriteAllText(modelConfigFileName, text);
		}
	}
}
