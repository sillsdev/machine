using System;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;

namespace SIL.Machine
{
	public class ParallelCorpusCommandSpec : CorpusCommandSpecBase
	{
		private CommandArgument _sourceArgument;
		private CommandArgument _targetArgument;
		private CommandOption _sourceFormatOption;
		private CommandOption _targetFormatOption;
		private CommandOption _alignmentsOption;
		private CommandOption _sourceWordTokenizerOption;
		private CommandOption _targetWordTokenizerOption;

		public bool SupportAlignmentsCorpus { get; set; } = true;
		public bool DefaultNullTokenizer { get; set; }
		public bool FilterSource { get; set; } = true;
		public bool FilterTarget { get; set; } = true;

		public ITextCorpus SourceCorpus { get; private set; }
		public ITextCorpus TargetCorpus { get; private set; }
		public ITextAlignmentCorpus AlignmentsCorpus { get; private set; }
		public ParallelTextCorpus ParallelCorpus { get; private set; }

		public override void AddParameters(CommandBase command)
		{
			_sourceArgument = command.Argument("SOURCE_PATH", "The source corpus.").IsRequired();
			_targetArgument = command.Argument("TARGET_PATH", "The target corpus.").IsRequired();

			_sourceFormatOption = command.Option("-sf|--source-format <CORPUS_FORMAT>",
				"The source corpus format.\nFormats: \"text\" (default), \"dbl\", \"usx\", \"pt\", \"pt-m\".",
				CommandOptionType.SingleValue);
			_targetFormatOption = command.Option("-tf|--target-format <CORPUS_FORMAT>",
				"The target corpus format.\nFormats: \"text\" (default), \"dbl\", \"usx\", \"pt\", \"pt-m\".",
				CommandOptionType.SingleValue);
			if (SupportAlignmentsCorpus)
			{
				_alignmentsOption = command.Option("-a|--alignments <ALIGNMENTS_PATH>",
					"The partial alignments corpus.", CommandOptionType.SingleValue);
			}

			string typesStr = "Types: \"whitespace\" (default), \"latin\", \"zwsp\"";
			if (DefaultNullTokenizer)
				typesStr = "Types: \"none\" (default), \"whitespace\", \"latin\", \"zwsp\"";
			_sourceWordTokenizerOption = command.Option("-st|--source-tokenizer <TOKENIZER>",
				$"The source word tokenizer.\n{typesStr}.",
				CommandOptionType.SingleValue);
			_targetWordTokenizerOption = command.Option("-tt|--target-tokenizer <TOKENIZER>",
				$"The target word tokenizer.\n{typesStr}.",
				CommandOptionType.SingleValue);

			base.AddParameters(command);
		}

		public override bool Validate(TextWriter outWriter)
		{
			if (!base.Validate(outWriter))
				return false;

			if (!ToolHelpers.ValidateCorpusFormatOption(_sourceFormatOption.Value()))
			{
				outWriter.WriteLine("The specified source corpus format is invalid.");
				return false;
			}

			if (!ToolHelpers.ValidateCorpusFormatOption(_targetFormatOption.Value()))
			{
				outWriter.WriteLine("The specified target corpus format is invalid.");
				return false;
			}

			if (!ToolHelpers.ValidateWordTokenizerOption(_sourceWordTokenizerOption.Value(),
				DefaultNullTokenizer))
			{
				outWriter.WriteLine("The specified source word tokenizer is invalid.");
				return false;
			}

			if (!ToolHelpers.ValidateWordTokenizerOption(_targetWordTokenizerOption.Value(),
				DefaultNullTokenizer))
			{
				outWriter.WriteLine("The specified target word tokenizer is invalid.");
				return false;
			}

			string defaultTokenizerType = DefaultNullTokenizer ? "none" : "whitespace";
			IRangeTokenizer<string, int, string> sourceWordTokenizer = ToolHelpers.CreateWordTokenizer(
				_sourceWordTokenizerOption.Value() ?? defaultTokenizerType);
			IRangeTokenizer<string, int, string> targetWordTokenizer = ToolHelpers.CreateWordTokenizer(
				_targetWordTokenizerOption.Value() ?? defaultTokenizerType);

			SourceCorpus = ToolHelpers.CreateTextCorpus(sourceWordTokenizer,
				_sourceFormatOption.Value() ?? "text", _sourceArgument.Value);
			TargetCorpus = ToolHelpers.CreateTextCorpus(targetWordTokenizer,
				_targetFormatOption.Value() ?? "text", _targetArgument.Value);
			AlignmentsCorpus = null;
			if (_alignmentsOption != null && _alignmentsOption.HasValue())
				AlignmentsCorpus = ToolHelpers.CreateAlignmentsCorpus("text", _alignmentsOption.Value());

			if (FilterSource)
				SourceCorpus = FilterTextCorpus(SourceCorpus);
			if (FilterTarget)
				TargetCorpus = FilterTextCorpus(TargetCorpus);
			if (AlignmentsCorpus != null)
				AlignmentsCorpus = FilterTextAlignmentCorpus(AlignmentsCorpus);

			ParallelCorpus = new ParallelTextCorpus(SourceCorpus, TargetCorpus, AlignmentsCorpus);
			return true;
		}

		public int GetNonemptyParallelCorpusCount()
		{
			return Math.Min(MaxCorpusCount, ParallelCorpus.Segments.Count(s => !s.IsEmpty));
		}

		public int GetParallelCorpusCount()
		{
			return Math.Min(MaxCorpusCount, ParallelCorpus.Segments.Count());
		}
	}
}
