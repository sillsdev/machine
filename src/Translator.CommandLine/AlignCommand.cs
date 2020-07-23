using System.IO;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine.Translation
{
	public class AlignCommand : EngineCommandBase
	{
		private readonly CommandOption _outputOption;
		private readonly CommandOption _probOption;
		private readonly CommandOption _quietOption;

		public AlignCommand()
			: base(true)
		{
			Name = "align";
			Description = "Generates word alignments for a parallel corpus.";

			_outputOption = Option("-o|--output <path>", "The output alignment directory.",
				CommandOptionType.SingleValue);
			_probOption = Option("-p|--probabilities", "Include probabilities in the output.",
				CommandOptionType.NoValue);
			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (!_outputOption.HasValue())
			{
				Out.WriteLine("The output alignment directory was not specified");
				return 1;
			}

			if (!Directory.Exists(_outputOption.Value()))
				Directory.CreateDirectory(_outputOption.Value());

			int parallelCorpusCount = GetParallelCorpusCount();

			string tmPrefix = Path.Combine(EngineDirectory, "tm", "src_trg");
			Out.Write("Aligning... ");
			using (var progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			using (var alignmentModel = new ThotSymmetrizedWordAlignmentModel(tmPrefix + "_invswm", tmPrefix + "_swm"))
			{
				int segmentCount = 0;
				progress?.Report(new ProgressStatus(segmentCount, parallelCorpusCount));
				foreach (ParallelText text in ParallelCorpus.Texts)
				{
					string fileName = Path.Combine(_outputOption.Value(), text.Id + ".txt");
					using (var writer = new StreamWriter(fileName))
					{
						foreach (ParallelTextSegment segment in text.Segments)
						{
							if (segment.IsEmpty)
							{
								writer.WriteLine();
							}
							else
							{
								writer.WriteLine(alignmentModel.GetAlignmentString(segment, _probOption.HasValue(),
									Preprocessors.Lowercase, Preprocessors.Lowercase));
								segmentCount++;
								progress?.Report(new ProgressStatus(segmentCount, parallelCorpusCount));
								if (segmentCount == MaxParallelCorpusCount)
									break;
							}
						}
					}
					if (segmentCount == MaxParallelCorpusCount)
						break;
				}
			}
			Out.WriteLine("done.");

			return 0;
		}
	}
}
