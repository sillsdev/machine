using System.Collections.Generic;
using System.IO;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace SIL.Machine
{
	public class ExtractCommand : CommandBase
	{
		private readonly ParallelCorpusCommandSpec _corpusSpec;
		private readonly CommandOption _sourceOutputOption;
		private readonly CommandOption _targetOutputOption;
		private readonly CommandOption _refOutputOption;
		private readonly CommandOption _allSourceOption;
		private readonly CommandOption _allTargetOption;
		private readonly CommandOption _lowercaseOption;
		private readonly CommandOption _includeEmptyOption;

		public ExtractCommand()
		{
			Name = "extract";
			Description = "Extracts a parallel corpus from source and target monolingual corpora.";

			_corpusSpec = AddSpec(new ParallelCorpusCommandSpec
			{
				SupportAlignmentsCorpus = false,
				DefaultNullTokenizer = true
			});
			_sourceOutputOption = Option("-so|--source-output <SOURCE_OUTPUT_FILE>", "The source output text file.",
				CommandOptionType.SingleValue);
			_targetOutputOption = Option("-to|--target-output <TARGET_OUTPUT_FILE>", "The target output text file.",
				CommandOptionType.SingleValue);
			_refOutputOption = Option("-ro|--ref-output <REF_OUTPUT_FILE>", "The segment reference output text file.",
				CommandOptionType.SingleValue);
			_allSourceOption = Option("-as|--all-source",
				"Include all source segments. Overrides include/exclude options.",
				CommandOptionType.NoValue);
			_allTargetOption = Option("-at|--all-target",
				"Include all target segments. Overrides include/exclude options.",
				CommandOptionType.NoValue);
			_lowercaseOption = Option("-l|--lowercase", "Convert text to lowercase.", CommandOptionType.NoValue);
			_includeEmptyOption = Option("-ie|--include-empty", "Include empty segments.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			_corpusSpec.FilterSource = !_allSourceOption.HasValue();
			_corpusSpec.FilterTarget = !_allTargetOption.HasValue();

			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (!_sourceOutputOption.HasValue() && !_targetOutputOption.HasValue() && !_refOutputOption.HasValue())
			{
				Out.WriteLine("An output file was not specified.");
				return 1;
			}

			int segmentCount = 0;
			var utf8Encoding = new UTF8Encoding(false);
			using (var sourceOutputWriter = _sourceOutputOption.HasValue()
				? new StreamWriter(_sourceOutputOption.Value(), false, utf8Encoding) : null)
			using (var targetOutputWriter = _targetOutputOption.HasValue()
				? new StreamWriter(_targetOutputOption.Value(), false, utf8Encoding) : null)
			using (var refOutputWriter = _refOutputOption.HasValue()
				? new StreamWriter(_refOutputOption.Value(), false, utf8Encoding) : null)
			{
				foreach (ParallelTextSegment segment in _corpusSpec.ParallelCorpus.GetSegments(
					_allSourceOption.HasValue(), _allTargetOption.HasValue()))
				{
					if (!_includeEmptyOption.HasValue())
					{
						if (_allSourceOption.HasValue() && _allTargetOption.HasValue())
						{
							if (segment.SourceSegment.Count == 0 && segment.TargetSegment.Count == 0)
								continue;
						}
						else if (_allSourceOption.HasValue())
						{
							if (segment.SourceSegment.Count == 0)
								continue;
						}
						else if (_allTargetOption.HasValue())
						{
							if (segment.TargetSegment.Count == 0)
								continue;
						}
						else if (segment.IsEmpty)
						{
							continue;
						}
					}

					var processorPipeline = new List<ITokenProcessor>
					{
						TokenProcessors.Normalize,
						TokenProcessors.EscapeSpaces
					};
					if (_lowercaseOption.HasValue())
						processorPipeline.Add(TokenProcessors.Lowercase);
					ITokenProcessor processor = TokenProcessors.Pipeline(processorPipeline);

					if (sourceOutputWriter != null)
					{
						if (segment.IsSourceInRange && !segment.IsSourceRangeStart && segment.SourceSegment.Count == 0)
						{
							sourceOutputWriter.WriteLine("<range>");
						}
						else
						{
							sourceOutputWriter.WriteLine(string.Join(" ", processor.Process(segment.SourceSegment)));
						}
					}
					if (targetOutputWriter != null)
					{
						if (segment.IsTargetInRange && !segment.IsTargetRangeStart && segment.TargetSegment.Count == 0)
						{
							targetOutputWriter.WriteLine("<range>");
						}
						else
						{
							targetOutputWriter.WriteLine(string.Join(" ", processor.Process(segment.TargetSegment)));
						}
					}
					refOutputWriter?.WriteLine(segment.SegmentRef);

					segmentCount++;
					if (segmentCount == _corpusSpec.MaxCorpusCount)
						break;
				}
			}

			Out.WriteLine($"# of Segments written: {segmentCount}");

			return 0;
		}
	}
}
