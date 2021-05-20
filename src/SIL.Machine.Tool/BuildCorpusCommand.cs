using System.IO;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace SIL.Machine
{
	public class BuildCorpusCommand : CommandBase
	{
		private readonly ParallelCorpusCommandSpec _corpusSpec;
		private readonly CommandOption _sourceOutputOption;
		private readonly CommandOption _targetOutputOption;
		private readonly CommandOption _refOutputOption;
		private readonly CommandOption _allSourceOption;
		private readonly CommandOption _allTargetOption;
		private readonly CommandOption _includeEmptyOption;
		private readonly PreprocessCommandSpec _preprocessSpec;

		public BuildCorpusCommand()
		{
			Name = "build-corpus";
			Description = "Builds a parallel corpus from source and target monolingual corpora.";

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
			_includeEmptyOption = Option("-ie|--include-empty", "Include empty segments.", CommandOptionType.NoValue);
			_preprocessSpec = AddSpec(new PreprocessCommandSpec { EscapeSpaces = true });
		}

		protected override async Task<int> ExecuteCommandAsync(CancellationToken ct)
		{
			_corpusSpec.FilterSource = !_allSourceOption.HasValue();
			_corpusSpec.FilterTarget = !_allTargetOption.HasValue();

			int code = await base.ExecuteCommandAsync(ct);
			if (code != 0)
				return code;

			if (!_sourceOutputOption.HasValue() && !_targetOutputOption.HasValue() && !_refOutputOption.HasValue())
			{
				Out.WriteLine("An output file was not specified.");
				return 1;
			}

			int segmentCount = 0;
			using (StreamWriter sourceOutputWriter = _sourceOutputOption.HasValue()
				? ToolHelpers.CreateStreamWriter(_sourceOutputOption.Value()) : null)
			using (StreamWriter targetOutputWriter = _targetOutputOption.HasValue()
				? ToolHelpers.CreateStreamWriter(_targetOutputOption.Value()) : null)
			using (StreamWriter refOutputWriter = _refOutputOption.HasValue()
				? ToolHelpers.CreateStreamWriter(_refOutputOption.Value()) : null)
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

					ITokenProcessor processor = _preprocessSpec.GetProcessor();

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
