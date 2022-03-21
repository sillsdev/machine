using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

namespace SIL.Machine
{
	public class TrainTranslationModelCommand : CommandBase
	{
		private readonly TranslationModelCommandSpec _modelSpec;
		private readonly ParallelCorpusCommandSpec _corpusSpec;
		private readonly PreprocessCommandSpec _preprocessSpec;
		private readonly CommandOption _quietOption;

		public TrainTranslationModelCommand()
		{
			Name = "translation-model";
			Description = "Trains a translation model from a parallel corpus.";

			_modelSpec = AddSpec(new TranslationModelCommandSpec());
			_corpusSpec = AddSpec(new ParallelCorpusCommandSpec());
			_preprocessSpec = AddSpec(new PreprocessCommandSpec());
			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override async Task<int> ExecuteCommandAsync(CancellationToken ct)
		{
			int code = await base.ExecuteCommandAsync(ct);
			if (code != 0)
				return code;

			IParallelTextCorpusView corpus = _preprocessSpec.Preprocess(_corpusSpec.ParallelCorpus);
			using (ITrainer trainer = _modelSpec.CreateTrainer(corpus, _corpusSpec.MaxCorpusCount))
			{
				if (!_quietOption.HasValue())
					Out.Write("Training... ");
				var watch = Stopwatch.StartNew();
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
				Out.WriteLine($"LM Perplexity: {trainer.Stats.Metrics["perplexity"]:0.0000}");
				Out.WriteLine($"TM BLEU: {trainer.Stats.Metrics["bleu"] * 100:0.00}");
			}

			return 0;
		}
	}
}
