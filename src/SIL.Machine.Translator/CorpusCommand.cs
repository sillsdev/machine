using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class CorpusCommand : ParallelTextCorpusCommandBase
	{
		private readonly CommandOption _maxLengthOption;
		private readonly CommandOption _countOption;

		public CorpusCommand()
			: base(supportAlignmentsCorpus: true, supportsNullTokenizer: true)
		{
			Name = "corpus";
			Description = "Computes statistics for a parallel corpus.";

			_countOption = Option("-c|--count", "Only output the # of parallel segments.", CommandOptionType.NoValue);
			_maxLengthOption = Option("--max-seglen <number>", "Maximum segment length. Default: 110.",
				CommandOptionType.SingleValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (_countOption.HasValue())
			{
				Out.WriteLine(GetParallelCorpusCount());
			}
			else
			{
				int maxLength = 110;
				if (_maxLengthOption.HasValue())
				{
					if (!int.TryParse(_maxLengthOption.Value(), out maxLength))
					{
						Out.WriteLine("The specified maximum segment length is invalid.");
						return 1;
					}
				}

				int textCount = 0;
				int segmentCount = 0;
				int sourceWordCount = 0;
				int targetWordCount = 0;
				foreach (ParallelText text in ParallelCorpus.Texts)
				{
					foreach (ParallelTextSegment segment in text.Segments.Where(s => !s.IsEmpty))
					{
						if (segment.SourceSegment.Count > maxLength)
						{
							Out.WriteLine($"Source segment \"{text.Id} {segment.SegmentRef}\" is too long, "
								+ $"length: {segment.SourceSegment.Count}");
						}
						if (segment.TargetSegment.Count > maxLength)
						{
							Out.WriteLine($"Target segment \"{text.Id} {segment.SegmentRef}\" is too long, "
								+ $"length: {segment.TargetSegment.Count}");
						}

						sourceWordCount += segment.SourceSegment.Count;
						targetWordCount += segment.TargetSegment.Count;
						segmentCount++;
						if (segmentCount == MaxParallelCorpusCount)
							break;
					}

					textCount++;
					if (segmentCount == MaxParallelCorpusCount)
						break;
				}

				Out.WriteLine($"# of Texts: {textCount}");
				Out.WriteLine($"# of Source Words: {sourceWordCount}");
				Out.WriteLine($"# of Target Words: {targetWordCount}");
				double avgSourceSegmentLength = (double) sourceWordCount / segmentCount;
				Out.WriteLine($"Avg. Source Segment Length: {avgSourceSegmentLength:#.##}");
				double avgTargetSegmentLength = (double) targetWordCount / segmentCount;
				Out.WriteLine($"Avg. Target Segment Length: {avgTargetSegmentLength:#.##}");
				Out.WriteLine($"# of Segments: {segmentCount}");
			}

			return 0;
		}
	}
}
