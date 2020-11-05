using System.Diagnostics;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Translation;

namespace SIL.Machine
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

			using (ITranslationModelTrainer trainer = _modelSpec.CreateTrainer(_corpusSpec.ParallelCorpus,
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
