using System;
using System.Collections.Generic;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

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
		private readonly bool _supportsNullTokenizer;

		protected ParallelTextCorpusCommandBase(bool supportAlignmentsCorpus, bool supportsNullTokenizer)
		{
			_supportsNullTokenizer = supportsNullTokenizer;

			_sourceOption = Option("-s|--source <[type,]path>",
				"The source corpus.\nTypes: \"text\" (default), \"dbl\", \"usx\", \"pt\".",
				CommandOptionType.SingleValue);
			_targetOption = Option("-t|--target <[type,]path>",
				"The target corpus.\nTypes: \"text\" (default), \"dbl\", \"usx\", \"pt\".",
				CommandOptionType.SingleValue);
			if (supportAlignmentsCorpus)
			{
				_alignmentsOption = Option("-a|--alignments <corpus>", "The partial alignments corpus.",
					CommandOptionType.SingleValue);
			}

			string typesStr = "Types: \"whitespace\" (default), \"latin\", \"zwsp\"";
			if (_supportsNullTokenizer)
				typesStr += ", \"null\"";
			_sourceWordTokenizerOption = Option("-st|--source-tokenizer <type>",
				$"The source word tokenizer type.\n{typesStr}.",
				CommandOptionType.SingleValue);
			_targetWordTokenizerOption = Option("-tt|--target-tokenizer <type>",
				$"The target word tokenizer type.\n{typesStr}.",
				CommandOptionType.SingleValue);
			_includeOption = Option("-i|--include <texts>",
				"The texts to include.\nFor Scripture, specify a book ID, \"*NT*\" for all NT books, or \"*OT*\" for all OT books.",
				CommandOptionType.MultipleValue);
			_excludeOption = Option("-e|--exclude <texts>",
				"The texts to exclude.\nFor Scripture, specify a book ID, \"*NT*\" for all NT books, or \"*OT*\" for all OT books.",
				CommandOptionType.MultipleValue);
			_maxCorpusSizeOption = Option("-m|--max-size <size>", "The maximum parallel corpus size.",
				CommandOptionType.SingleValue);
		}

		protected ITextCorpus SourceCorpus { get; private set; }
		protected ITextCorpus TargetCorpus { get; private set; }
		protected ITextAlignmentCorpus AlignmentsCorpus { get; private set; }
		protected ParallelTextCorpus ParallelCorpus { get; private set; }
		protected int MaxParallelCorpusCount { get; private set; } = int.MaxValue;
		protected virtual bool FilterSource { get; } = true;
		protected virtual bool FilterTarget { get; } = true;

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

			if (!TranslatorHelpers.ValidateTextCorpusOption(_sourceOption.Value(), out string sourceType,
				out string sourcePath))
			{
				Out.WriteLine("The specified source corpus is invalid.");
				return 1;
			}

			if (!TranslatorHelpers.ValidateTextCorpusOption(_targetOption.Value(), out string targetType,
				out string targetPath))
			{
				Out.WriteLine("The specified target corpus is invalid.");
				return 1;
			}

			string alignmentsType = null, alignmentsPath = null;
			if (_alignmentsOption != null && !TranslatorHelpers.ValidateAlignmentsOption(_alignmentsOption.Value(),
				out alignmentsType, out alignmentsPath))
			{
				Out.WriteLine("The specified partial alignments corpus is invalid.");
				return 1;
			}

			if (!TranslatorHelpers.ValidateWordTokenizerOption(_sourceWordTokenizerOption.Value(),
				_supportsNullTokenizer))
			{
				Out.WriteLine("The specified source word tokenizer type is invalid.");
				return 1;
			}

			if (!TranslatorHelpers.ValidateWordTokenizerOption(_targetWordTokenizerOption.Value(),
				_supportsNullTokenizer))
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

			ITokenizer<string, int, string> sourceWordTokenizer = TranslatorHelpers.CreateWordTokenizer(
				_sourceWordTokenizerOption.Value());
			ITokenizer<string, int, string> targetWordTokenizer = TranslatorHelpers.CreateWordTokenizer(
				_targetWordTokenizerOption.Value());

			SourceCorpus = TranslatorHelpers.CreateTextCorpus(sourceWordTokenizer, sourceType, sourcePath);
			TargetCorpus = TranslatorHelpers.CreateTextCorpus(targetWordTokenizer, targetType, targetPath);
			AlignmentsCorpus = null;
			if (_alignmentsOption != null && _alignmentsOption.HasValue())
				AlignmentsCorpus = TranslatorHelpers.CreateAlignmentsCorpus(alignmentsType, alignmentsPath);

			ISet<string> includeTexts = null;
			if (_includeOption.HasValue())
				includeTexts = TranslatorHelpers.GetTexts(_includeOption.Values);

			ISet<string> excludeTexts = null;
			if (_excludeOption.HasValue())
				excludeTexts = TranslatorHelpers.GetTexts(_excludeOption.Values);

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

				if (FilterSource)
					SourceCorpus = new FilteredTextCorpus(SourceCorpus, text => Filter(text.Id));
				if (FilterTarget)
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
	}
}
