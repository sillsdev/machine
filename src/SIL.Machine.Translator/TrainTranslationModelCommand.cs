using System;
using System.Diagnostics;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine.Translation
{
	public class TrainTranslationModelCommand : CommandBase
	{
		private readonly TranslationModelCommandSpec _modelSpec;
		private readonly ParallelCorpusCommandSpec _corpusSpec;
		private readonly CommandOption _quietOption;

		public TrainTranslationModelCommand()
		{
			Name = "translation-model";
			Description = "Trains a translation model from a parallel corpus.";

			_modelSpec = AddSpec(new TranslationModelCommandSpec());
			_corpusSpec = AddSpec(new ParallelCorpusCommandSpec());

			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (!Directory.Exists(_modelSpec.ModelDirectory))
				Directory.CreateDirectory(_modelSpec.ModelDirectory);

			if (!File.Exists(_modelSpec.ModelConfigFileName))
			{
				string defaultConfigFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data",
					"default-smt.cfg");
				File.Copy(defaultConfigFileName, _modelSpec.ModelConfigFileName);
			}

			using (ITranslationModelTrainer trainer = new ThotSmtModelTrainer(_modelSpec.ModelConfigFileName,
				TokenProcessors.Lowercase, TokenProcessors.Lowercase, _corpusSpec.ParallelCorpus,
				_corpusSpec.MaxCorpusCount))
			{
				Stopwatch watch = Stopwatch.StartNew();
				if (!_quietOption.HasValue())
					Out.Write("Training... ");
				using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
				{
					var reporter = new PhasedProgressReporter(progress,
						new Phase("Training model", 0.99),
						new Phase("Saving model"));
					using (PhaseProgress phaseProgress = reporter.StartNextPhase())
						trainer.Train(phaseProgress);
					using (PhaseProgress phaseProgress = reporter.StartNextPhase())
						trainer.Save();
				}
				if (!_quietOption.HasValue())
					Out.WriteLine("done.");
				watch.Stop();

				Out.WriteLine($"Execution time: {watch.Elapsed:c}");
				Out.WriteLine($"# of Segments Trained: {trainer.Stats.TrainedSegmentCount}");
				Out.WriteLine($"LM Perplexity: {trainer.Stats.LanguageModelPerplexity:0.0000}");
				Out.WriteLine($"TM BLEU: {trainer.Stats.TranslationModelBleu * 100:0.00}");
			}

			return 0;
		}
	}
}
