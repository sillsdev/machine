using McMaster.Extensions.CommandLineUtils;
using SIL.CommandLine;
using SIL.Machine.Corpora;
using SIL.Machine.Translation.Thot;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class TrainCommand : EngineCommandBase
	{
		private readonly CommandOption _alignmentOnlyOption;
		private readonly CommandOption _quietOption;

		public TrainCommand()
			: base(true)
		{
			Name = "train";

			_alignmentOnlyOption = Option("--alignment-only", "Only train the alignment models.",
				CommandOptionType.NoValue);
			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
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
				Preprocessors.Lowercase, ParallelCorpus, MaxParallelCorpusCount))
			{
				Stopwatch watch = Stopwatch.StartNew();
				if (!_quietOption.HasValue())
					Out.Write("Training... ");
				using (ConsoleProgressBar<SmtTrainProgress> progress = _quietOption.HasValue() ? null
					: new ConsoleProgressBar<SmtTrainProgress>(Out, p => (double) p.CurrentStep / p.StepCount))
				{
					trainer.Train(progress);
					trainer.Save();
				}
				if (!_quietOption.HasValue())
					Out.WriteLine("done.");
				watch.Stop();

				Out.WriteLine($"Execution time: {watch.Elapsed:c}");
				Out.WriteLine($"# of Segments Trained: {trainer.Stats.TrainedSegmentCount}");
				Out.WriteLine($"LM Perplexity: {trainer.Stats.LanguageModelPerplexity:0.0000}");
				Out.WriteLine($"TM BLEU: {trainer.Stats.TranslationModelBleu:0.0000}");
			}
		}

		private void TrainAlignmentModels()
		{
			string tmDir = Path.Combine(EngineDirectory, "tm");
			if (!Directory.Exists(tmDir))
				Directory.CreateDirectory(tmDir);

			string tmPrefix = Path.Combine(tmDir, "src_trg");
			int parallelCorpusCount = GetParallelCorpusCount();

			if (!_quietOption.HasValue())
				Out.Write("Training... ");
			using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			{
				TrainAlignmentModel(tmPrefix + "_swm", ParallelCorpus.Invert(), progress);
				TrainAlignmentModel(tmPrefix + "_invswm", ParallelCorpus, progress, 5);
			}
			if (!_quietOption.HasValue())
				Out.WriteLine("done.");

			Out.WriteLine($"# of Segments Trained: {parallelCorpusCount}");
		}

		private void TrainAlignmentModel(string swmPrefix, ParallelTextCorpus corpus,
			IProgress<double> progress = null, int startStep = 0)
		{
			using (var swAlignModel = new ThotSingleWordAlignmentModel(swmPrefix, true))
			{
				foreach (ParallelTextSegment segment in corpus.Segments.Where(s => !s.IsEmpty)
					.Take(MaxParallelCorpusCount))
				{
					string[] sourceTokens = segment.SourceSegment.Select(Preprocessors.Lowercase).ToArray();
					string[] targetTokens = segment.TargetSegment.Select(Preprocessors.Lowercase).ToArray();
					swAlignModel.AddSegmentPair(sourceTokens, targetTokens, segment.CreateAlignmentMatrix(true));
				}

				for (int i = 0; i < 5; i++)
				{
					swAlignModel.Train(1);
					progress?.Report((double) (startStep + i + 1) / 10);
				}
				swAlignModel.Save();
			}
		}
	}
}
