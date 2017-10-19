using McMaster.Extensions.CommandLineUtils;
using SIL.CommandLine;
using SIL.Machine.Corpora;
using SIL.Machine.Translation.Thot;
using System;
using System.IO;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class TrainCommand : EngineCommandBase
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

			if (_alignmentOnlyOption.HasValue())
				TrainAlignmentModels();
			else
				TrainSmtModel();

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

			using (ISmtBatchTrainer trainer = new ThotSmtBatchTrainer(EngineConfigFileName, Preprocessors.Lowercase,
				SourceCorpus, Preprocessors.Lowercase, TargetCorpus, AlignmentsCorpus))
			{
				Out.Write("Training... ");
				using (var progress = new ConsoleProgressBar<SmtTrainProgress>(Out,
					p => (double) p.CurrentStep / p.StepCount))
				{
					trainer.Train(progress);
					trainer.Save();
				}
				Out.WriteLine("done.");

				Out.WriteLine($"# of Segments Trained: {trainer.Stats.TrainedSegmentCount}");
				Out.WriteLine($"LM Perplexity: {trainer.Stats.LanguageModelPerplexity:0.00}");
				Out.WriteLine($"TM BLEU: {trainer.Stats.TranslationModelBleu:0.00}");
			}
		}

		private void TrainAlignmentModels()
		{
			string tmDir = Path.Combine(EngineDirectory, "tm");
			if (!Directory.Exists(tmDir))
				Directory.CreateDirectory(tmDir);

			string tmPrefix = Path.Combine(tmDir, "src_trg");
			var corpus = new ParallelTextCorpus(SourceCorpus, TargetCorpus, AlignmentsCorpus);
			int totalSegmentCount = corpus.Texts.SelectMany(t => t.Segments).Count(s => !s.IsEmpty);

			Out.Write("Training... ");
			using (var progress = new ConsoleProgressBar(Out))
			{
				TrainAlignmentModel(tmPrefix + "_swm", corpus.Invert(), progress);
				TrainAlignmentModel(tmPrefix + "_invswm", corpus, progress, 5);
			}
			Out.WriteLine("done.");

			Out.WriteLine($"# of Segments Trained: {totalSegmentCount}");
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
