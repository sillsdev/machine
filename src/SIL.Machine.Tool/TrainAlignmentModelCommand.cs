using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Translation;

namespace SIL.Machine
{
	public class TrainAlignmentModelCommand : CommandBase
	{
		private readonly AlignmentModelCommandSpec _modelSpec;
		private readonly ParallelCorpusCommandSpec _corpusSpec;
		private readonly CommandOption _quietOption;

		public TrainAlignmentModelCommand()
		{
			Name = "alignment-model";
			Description = "Trains a word alignment model from a parallel corpus.";

			_modelSpec = AddSpec(new AlignmentModelCommandSpec { IncludeSymHeuristicOption = false });
			_corpusSpec = AddSpec(new ParallelCorpusCommandSpec());

			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			int parallelCorpusCount = _corpusSpec.GetNonemptyParallelCorpusCount();

			if (!_quietOption.HasValue())
				Out.Write("Training... ");
			using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			using (ITrainer trainer = _modelSpec.CreateAlignmentModelTrainer(_corpusSpec.ParallelCorpus,
				_corpusSpec.MaxCorpusCount))
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

			Out.WriteLine($"# of Segments Trained: {parallelCorpusCount}");

			return 0;
		}
	}
}
