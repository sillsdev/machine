using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace SIL.Machine
{
	public class CorpusStatsCommand : CommandBase
	{
		private readonly ParallelCorpusCommandSpec _corpusSpec;
		private readonly CommandOption _maxLengthOption;
		private readonly CommandOption _countOption;

		public CorpusStatsCommand()
		{
			Name = "corpus-stats";
			Description = "Computes statistics for a parallel corpus.";

			_corpusSpec = AddSpec(new ParallelCorpusCommandSpec());
			_countOption = Option("-c|--count", "Only output the # of parallel segments.", CommandOptionType.NoValue);
			_maxLengthOption = Option("-ms|--max-seglen <MAX_SEG_LENGTH>",
				$"Maximum segment length. Default: {TranslationConstants.MaxSegmentLength}.",
				CommandOptionType.SingleValue);
		}

		protected override async Task<int> ExecuteCommandAsync(CancellationToken ct)
		{
			int code = await base.ExecuteCommandAsync(ct);
			if (code != 0)
				return code;

			if (_countOption.HasValue())
			{
				Out.WriteLine(Math.Min(_corpusSpec.MaxCorpusCount, _corpusSpec.ParallelCorpus.Count()));
			}
			else
			{
				int maxLength = TranslationConstants.MaxSegmentLength;
				if (_maxLengthOption.HasValue())
				{
					if (!int.TryParse(_maxLengthOption.Value(), out maxLength))
					{
						Out.WriteLine("The specified maximum segment length is invalid.");
						return 1;
					}
				}

				int segmentCount = 0;
				int sourceWordCount = 0;
				int targetWordCount = 0;
				IEnumerable<ParallelTextRow> corpus = _corpusSpec.ParallelCorpus
					.Where(r => !r.IsEmpty)
					.Take(_corpusSpec.MaxCorpusCount);
				foreach (ParallelTextRow row in corpus)
				{
					if (row.SourceSegment.Count > maxLength)
					{
						Out.WriteLine($"Source segment \"{row.Ref}\" is too long, "
							+ $"length: {row.SourceSegment.Count}");
					}
					if (row.TargetSegment.Count > maxLength)
					{
						Out.WriteLine($"Target segment \"{row.Ref}\" is too long, "
							+ $"length: {row.TargetSegment.Count}");
					}

					sourceWordCount += row.SourceSegment.Count;
					targetWordCount += row.TargetSegment.Count;
					segmentCount++;
					if (segmentCount == _corpusSpec.MaxCorpusCount)
						break;
				}

				Out.WriteLine($"# of Source Words: {sourceWordCount}");
				Out.WriteLine($"# of Target Words: {targetWordCount}");
				double avgSourceSegmentLength = (double)sourceWordCount / segmentCount;
				Out.WriteLine($"Avg. Source Segment Length: {avgSourceSegmentLength:#.##}");
				double avgTargetSegmentLength = (double)targetWordCount / segmentCount;
				Out.WriteLine($"Avg. Target Segment Length: {avgTargetSegmentLength:#.##}");
				Out.WriteLine($"# of Segments: {segmentCount}");
			}

			return 0;
		}
	}
}
