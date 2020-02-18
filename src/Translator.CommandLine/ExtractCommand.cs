using System;
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
		private readonly CommandOption _allSourceSegmentsOption;
		private readonly CommandOption _allTargetSegmentsOption;
		private readonly CommandOption _lowercaseOption;

		public ExtractCommand()
			: base(false)
		{
			Name = "extract";

			_sourceOutputOption = Option("-so|--source-output <path>", "The source output file.",
				CommandOptionType.SingleValue);
			_targetOutputOption = Option("-to|--target-output <path>", "The target output file.",
				CommandOptionType.SingleValue);
			_allSourceSegmentsOption = Option("-as|--all-source-segments", "Include all source segments.",
				CommandOptionType.NoValue);
			_allTargetSegmentsOption = Option("-at|--all-target-segments", "Include all target segments.",
				CommandOptionType.NoValue);
			_lowercaseOption = Option("-l|--lowercase", "Convert text to lowercase.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			int segmentCount = 0;
			var utf8Encoding = new UTF8Encoding(false);
			using (var sourceOutputWriter = _sourceOutputOption.HasValue()
				? new StreamWriter(_sourceOutputOption.Value(), false, utf8Encoding) : null)
			using (var targetOutputWriter = _targetOutputOption.HasValue()
				? new StreamWriter(_targetOutputOption.Value(), false, utf8Encoding) : null)
			{
				foreach (ParallelText text in ParallelCorpus.Texts)
				{
					foreach (ParallelTextSegment segment in text.GetSegments(_allSourceSegmentsOption.HasValue(),
						_allTargetSegmentsOption.HasValue()))
					{
						if (_allSourceSegmentsOption.HasValue() && _allTargetSegmentsOption.HasValue())
						{
							if (segment.SourceSegment.Count == 0 && segment.TargetSegment.Count == 0)
								continue;
						}
						else if (_allSourceSegmentsOption.HasValue())
						{
							if (segment.SourceSegment.Count == 0)
								continue;
						}
						else if (_allTargetSegmentsOption.HasValue())
						{
							if (segment.TargetSegment.Count == 0)
								continue;
						}
						else if (segment.IsEmpty)
						{
							continue;
						}

						Func<string, string> preprocessor = Preprocessors.Null;
						if (_lowercaseOption.HasValue())
							preprocessor = Preprocessors.Lowercase;

						if (sourceOutputWriter != null)
						{
							sourceOutputWriter.WriteLine(string.Join(" ", segment.SourceSegment
								.Preprocess(preprocessor)));
						}
						if (targetOutputWriter != null)
						{
							targetOutputWriter.WriteLine(string.Join(" ", segment.TargetSegment
								.Preprocess(preprocessor)));
						}

						segmentCount++;
						if (segmentCount == MaxParallelCorpusCount)
							break;
					}
					if (segmentCount == MaxParallelCorpusCount)
						break;
				}
			}

			Out.WriteLine($"# of Segments written: {segmentCount}");

			return 0;
		}
	}
}
