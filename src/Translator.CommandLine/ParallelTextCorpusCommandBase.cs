using McMaster.Extensions.CommandLineUtils;
using SIL.Extensions;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public abstract class ParallelTextCorpusCommandBase : CommandBase
	{
		private readonly CommandOption _sourceOption;
		private readonly CommandOption _targetOption;
		private readonly CommandOption _alignmentsOption;
		private readonly CommandOption _sourceWordTokenizerOption;
		private readonly CommandOption _targetWordTokenizerOption;
		private readonly CommandOption _includeOption;
		private readonly CommandOption _excludeOption;
		private readonly CommandOption _maxCorpusSizeOption;

		protected ParallelTextCorpusCommandBase(bool supportAlignmentsCorpus)
		{
			_sourceOption = Option("-s|--source <corpus>", "The source corpus.", CommandOptionType.SingleValue);
			_targetOption = Option("-t|--target <corpus>", "The target corpus.", CommandOptionType.SingleValue);
			if (supportAlignmentsCorpus)
			{
				_alignmentsOption = Option("-a|--alignments <corpus>", "The partial alignments corpus.",
					CommandOptionType.SingleValue);
			}
			_sourceWordTokenizerOption = Option("-st|--source-tokenizer <type>", "The source word tokenizer type.",
				CommandOptionType.SingleValue);
			_targetWordTokenizerOption = Option("-tt|--target-tokenizer <type>", "The target word tokenizer type.",
				CommandOptionType.SingleValue);
			_includeOption = Option("-i|--include <texts>", "The texts to include.", CommandOptionType.MultipleValue);
			_excludeOption = Option("-e|--exclude <texts>", "The texts to exclude.", CommandOptionType.MultipleValue);
			_maxCorpusSizeOption = Option("-m|--max-size <size>", "The maximum parallel corpus size.",
				CommandOptionType.SingleValue);
		}

		protected ITextCorpus SourceCorpus { get; private set; }
		protected ITextCorpus TargetCorpus { get; private set; }
		protected ITextAlignmentCorpus AlignmentsCorpus { get; private set; }
		protected ParallelTextCorpus ParallelCorpus { get; private set; }
		protected int MaxParallelCorpusCount { get; private set; } = int.MaxValue;

		protected override int ExecuteCommand()
		{
			if (!_sourceOption.HasValue())
			{
				Out.WriteLine("The source corpus was not specified.");
				return 1;
			}

			if (!_targetOption.HasValue())
			{
				Out.WriteLine("The target corpus was not specified.");
				return 1;
			}

			if (!ValidateTextCorpusOption(_sourceOption.Value(), out string sourceType, out string sourcePath))
			{
				Out.WriteLine("The specified source corpus is invalid.");
				return 1;
			}

			if (!ValidateTextCorpusOption(_targetOption.Value(), out string targetType, out string targetPath))
			{
				Out.WriteLine("The specified target corpus is invalid.");
				return 1;
			}

			string alignmentsType = null, alignmentsPath = null;
			if (_alignmentsOption != null && !ValidateAlignmentsOption(_alignmentsOption.Value(), out alignmentsType,
				out alignmentsPath))
			{
				Out.WriteLine("The specified partial alignments corpus is invalid.");
				return 1;
			}

			if (!ValidateWordTokenizerOption(_sourceWordTokenizerOption.Value()))
			{
				Out.WriteLine("The specified source word tokenizer type is invalid.");
				return 1;
			}

			if (!ValidateWordTokenizerOption(_targetWordTokenizerOption.Value()))
			{
				Out.WriteLine("The specified target word tokenizer type is invalid.");
				return 1;
			}

			if (_maxCorpusSizeOption.HasValue())
			{
				if (!int.TryParse(_maxCorpusSizeOption.Value(), out int maxCorpusSize) || maxCorpusSize <= 0)
				{
					Out.WriteLine("The specified maximum corpus size is invalid.");
					return 1;
				}
				MaxParallelCorpusCount = maxCorpusSize;
			}

			StringTokenizer sourceWordTokenizer = CreateWordTokenizer(_sourceWordTokenizerOption.Value());
			StringTokenizer targetWordTokenizer = CreateWordTokenizer(_targetWordTokenizerOption.Value());

			SourceCorpus = CreateTextCorpus(sourceWordTokenizer, sourceType, sourcePath);
			TargetCorpus = CreateTextCorpus(targetWordTokenizer, targetType, targetPath);
			AlignmentsCorpus = null;
			if (_alignmentsOption != null && _alignmentsOption.HasValue())
				AlignmentsCorpus = CreateAlignmentsCorpus(alignmentsType, alignmentsPath);

			ISet<string> includeTexts = null;
			if (_includeOption.HasValue())
				includeTexts = GetTexts(_includeOption.Values);

			ISet<string> excludeTexts = null;
			if (_excludeOption.HasValue())
				excludeTexts = GetTexts(_excludeOption.Values);

			if (includeTexts != null || excludeTexts != null)
			{
				bool Filter(string id)
				{
					if (excludeTexts != null && excludeTexts.Contains(id))
						return false;

					if (includeTexts != null && includeTexts.Contains(id))
						return true;

					return includeTexts == null;
				}

				SourceCorpus = new FilteredTextCorpus(SourceCorpus, text => Filter(text.Id));
				TargetCorpus = new FilteredTextCorpus(TargetCorpus, text => Filter(text.Id));
				if (_alignmentsOption != null && _alignmentsOption.HasValue())
				{
					AlignmentsCorpus = new FilteredTextAlignmentCorpus(AlignmentsCorpus,
						alignments => Filter(alignments.Id));
				}
			}

			ParallelCorpus = new ParallelTextCorpus(SourceCorpus, TargetCorpus, AlignmentsCorpus);

			return 0;
		}

		protected int GetParallelCorpusCount()
		{
			return Math.Min(MaxParallelCorpusCount, ParallelCorpus.Segments.Count(s => !s.IsEmpty));
		}

		private static bool ValidateCorpusOption(string value, out string type, out string path)
		{
			type = null;
			path = null;

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

		private static bool ValidateTextCorpusOption(string value, out string type, out string path)
		{
			if (ValidateCorpusOption(value, out type, out path))
				return string.IsNullOrEmpty(type) || type.IsOneOf("dbl", "usx", "text");
			return false;
		}

		private static ITextCorpus CreateTextCorpus(StringTokenizer wordTokenizer, string type, string path)
		{
			switch (type)
			{
				case "dbl":
					return new DblBundleTextCorpus(wordTokenizer, path);

				case "usx":
					return new UsxFileTextCorpus(wordTokenizer, path);

				case "text":
				default:
					return new TextFileTextCorpus(wordTokenizer, path);
			}
		}

		private static bool ValidateAlignmentsOption(string value, out string type, out string path)
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

		private static ITextAlignmentCorpus CreateAlignmentsCorpus(string type, string path)
		{
			switch (type)
			{
				case "text":
				default:
					return new TextFileTextAlignmentCorpus(path);
			}
		}

		private static bool ValidateWordTokenizerOption(string value)
		{
			return string.IsNullOrEmpty(value) || value.IsOneOf("latin", "whitespace");
		}

		private static StringTokenizer CreateWordTokenizer(string type)
		{
			switch (type)
			{
				case "latin":
					return new LatinWordTokenizer();

				case "whitespace":
				default:
					return new WhitespaceTokenizer();
			}
		}

		private static ISet<string> GetTexts(IEnumerable<string> values)
		{
			return new HashSet<string>(values.SelectMany(value =>
				value.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)));
		}
	}
}
