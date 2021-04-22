using System.Diagnostics;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Translation;

namespace SIL.Machine
{
	public class TrainAlignmentModelCommand : CommandBase
	{
		private readonly AlignmentModelCommandSpec _modelSpec;
		private readonly ParallelCorpusCommandSpec _corpusSpec;
		private readonly CommandOption _trainParamsOption;
		private readonly PreprocessCommandSpec _preprocessSpec;
		private readonly CommandOption _quietOption;

		public TrainAlignmentModelCommand()
		{
			Name = "alignment-model";
			Description = "Trains a word alignment model from a parallel corpus.";

			_modelSpec = AddSpec(new AlignmentModelCommandSpec());
			_corpusSpec = AddSpec(new ParallelCorpusCommandSpec());
			_trainParamsOption = Option("-tp|--training-params", "Model training parameters.\nParameter format: \"<key>=<value>\".",
				CommandOptionType.MultipleValue);
			_preprocessSpec = AddSpec(new PreprocessCommandSpec());
			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (_trainParamsOption.Values.Any(kvp => !kvp.Contains("=")))
			{
				Out.WriteLine("The training parameters are not formatted correctly.");
				return 1;
			}

			var parameters = _trainParamsOption.Values.Select(kvp => kvp.Split("="))
				.ToDictionary(kvp => kvp[0].ToLowerInvariant(), kvp => kvp[1]);

			int parallelCorpusCount = _corpusSpec.GetNonemptyParallelCorpusCount();

			Stopwatch watch = Stopwatch.StartNew();
			if (!_quietOption.HasValue())
				Out.Write("Training... ");
			using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			using (ITrainer trainer = _modelSpec.CreateAlignmentModelTrainer(_corpusSpec.ParallelCorpus,
				_corpusSpec.MaxCorpusCount, _preprocessSpec.GetProcessor(), parameters))
			{
				var reporter = new PhasedProgressReporter(progress,
					new Phase("Training model", 0.95),
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
			Out.WriteLine($"# of Segments Trained: {parallelCorpusCount}");

			return 0;
		}
	}
}
