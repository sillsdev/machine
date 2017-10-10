using McMaster.Extensions.CommandLineUtils;
using SIL.CommandLine;
using SIL.Machine.Corpora;
using SIL.Machine.Translation.Thot;
using System;
using System.IO;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class TrainCommand : ParallelTextCommand
	{
		private readonly CommandOption _alignmentOnlyOption;

		public TrainCommand()
			: base(true)
		{
			Name = "train";

			_alignmentOnlyOption = Option("--alignment-only", "Only train the alignment models.",
				CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (!Directory.Exists(EngineDirectory))
				Directory.CreateDirectory(EngineDirectory);

			Out.Write("Training... ");
			if (_alignmentOnlyOption.HasValue())
				TrainAlignmentModels();
			else
				TrainSmtModel();
			Out.WriteLine("done.");

			return 0;
		}

		private void TrainSmtModel()
		{
			if (!File.Exists(EngineConfigFileName))
			{
				string defaultConfigFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data",
					"default-smt.cfg");
				File.Copy(defaultConfigFileName, EngineConfigFileName);
			}

			using (var progress = new ConsoleProgressBar<SmtTrainProgress>(Out,
				p => (double) p.CurrentStep / p.StepCount))
			using (ISmtBatchTrainer trainer = new ThotSmtBatchTrainer(EngineConfigFileName, Preprocessors.Lowercase,
				SourceCorpus, Preprocessors.Lowercase, TargetCorpus, AlignmentsCorpus))
			{
				trainer.Train(progress);
				trainer.Save();
			}
		}

		private void TrainAlignmentModels()
		{
			string tmDir = Path.Combine(EngineDirectory, "tm");
			if (!Directory.Exists(tmDir))
				Directory.CreateDirectory(tmDir);

			string tmPrefix = Path.Combine(tmDir, "src_trg");
			var corpus = new ParallelTextCorpus(SourceCorpus, TargetCorpus, AlignmentsCorpus);
			using (var progress = new ConsoleProgressBar(Out))
			{
				TrainAlignmentModel(tmPrefix + "_swm", corpus.Invert(), progress);
				TrainAlignmentModel(tmPrefix + "_invswm", corpus, progress, 5);
			}
		}

		private static void TrainAlignmentModel(string swmPrefix, ParallelTextCorpus corpus,
			IProgress<double> progress, int startStep = 0)
		{
			using (var swAlignModel = new ThotSingleWordAlignmentModel(swmPrefix, true))
			{
				foreach (ParallelTextSegment segment in corpus.Segments.Where(s => !s.IsEmpty))
				{
					string[] sourceTokens = segment.SourceSegment.Select(Preprocessors.Lowercase).ToArray();
					string[] targetTokens = segment.TargetSegment.Select(Preprocessors.Lowercase).ToArray();
					swAlignModel.AddSegmentPair(sourceTokens, targetTokens, segment.CreateAlignmentMatrix(true));
				}

				for (int i = 0; i < 5; i++)
				{
					swAlignModel.Train(1);
					progress.Report((double) (startStep + i + 1) / 10);
				}
				swAlignModel.Save();
			}
		}
	}
}
