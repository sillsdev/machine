using System.IO;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
	public class ExtractCommand : ParallelTextCorpusCommandBase
	{
		private readonly CommandOption _sourceOutputOption;
		private readonly CommandOption _targetOutputOption;
		private readonly CommandOption _allSourceOption;
		private readonly CommandOption _allTargetOption;
		private readonly CommandOption _lowercaseOption;
		private readonly CommandOption _includeEmptyOption;

		public ExtractCommand()
			: base(supportAlignmentsCorpus: false, defaultNullTokenizer: true)
		{
			Name = "extract";
			Description = "Extracts a parallel corpus from source and target monolingual corpora.";

			_sourceOutputOption = Option("-so|--source-output <path>", "The source output file.",
				CommandOptionType.SingleValue);
			_targetOutputOption = Option("-to|--target-output <path>", "The target output file.",
				CommandOptionType.SingleValue);
			_allSourceOption = Option("-as|--all-source",
				"Include all source segments. This parameter overrides all other include/exclude parameters.",
				CommandOptionType.NoValue);
			_allTargetOption = Option("-at|--all-target",
				"Include all target segments. This parameter overrides all other include/exclude parameters.",
				CommandOptionType.NoValue);
			_lowercaseOption = Option("-l|--lowercase", "Convert text to lowercase.", CommandOptionType.NoValue);
			_includeEmptyOption = Option("-ie|--include-empty", "Include empty segments.",
				CommandOptionType.NoValue);
		}

		protected override bool FilterSource => !_allSourceOption.HasValue();
		protected override bool FilterTarget => !_allTargetOption.HasValue();

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (!_sourceOutputOption.HasValue() && !_targetOutputOption.HasValue())
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
			{
				foreach (ParallelTextSegment segment in ParallelCorpus.GetSegments(_allSourceOption.HasValue(),
						_allTargetOption.HasValue()))
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

					ITokenProcessor preprocessor = TokenProcessors.Null;
					if (_lowercaseOption.HasValue())
						preprocessor = TokenProcessors.Lowercase;

					if (sourceOutputWriter != null)
					{
						if (segment.IsSourceInRange && segment.SourceSegment.Count == 0)
						{
							sourceOutputWriter.WriteLine("<range>");
						}
						else
						{
							sourceOutputWriter.WriteLine(string.Join(" ", TokenProcessors.Pipeline(preprocessor,
								TokenProcessors.EscapeSpaces).Process(segment.SourceSegment)));
						}
					}
					if (targetOutputWriter != null)
					{
						if (segment.IsTargetInRange && segment.TargetSegment.Count == 0)
						{
							targetOutputWriter.WriteLine("<range>");
						}
						else
						{
							targetOutputWriter.WriteLine(string.Join(" ", TokenProcessors.Pipeline(preprocessor,
								TokenProcessors.EscapeSpaces).Process(segment.TargetSegment)));
						}
					}

					segmentCount++;
					if (segmentCount == MaxParallelCorpusCount)
						break;
				}
			}

			Out.WriteLine($"# of Segments written: {segmentCount}");

			return 0;
		}
	}
}
