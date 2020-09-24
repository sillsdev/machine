using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Translation.Paratext;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine.Translation
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

			_modelSpec = AddSpec(new AlignmentModelCommandSpec());
			_corpusSpec = AddSpec(new ParallelCorpusCommandSpec());

			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			string modelDir = Path.GetDirectoryName(_modelSpec.ModelPath);
			if (!Directory.Exists(modelDir))
				Directory.CreateDirectory(modelDir);

			int parallelCorpusCount = _corpusSpec.GetNonemptyParallelCorpusCount();

			if (!_quietOption.HasValue())
				Out.Write("Training... ");
			using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			using (ITrainer trainer = CreateAlignmentModelTrainer())
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

		private ITrainer CreateAlignmentModelTrainer()
		{
			switch (_modelSpec.ModelType)
			{
				case "hmm":
					return CreateThotAlignmentModelTrainer<HmmThotWordAlignmentModel>();
				case "ibm1":
					return CreateThotAlignmentModelTrainer<Ibm1ThotWordAlignmentModel>();
				case "ibm2":
					return CreateThotAlignmentModelTrainer<Ibm2ThotWordAlignmentModel>();
				case "pt":
					return new ParatextWordAlignmentModelTrainer(_modelSpec.ModelPath, TokenProcessors.Lowercase,
						TokenProcessors.Lowercase, _corpusSpec.ParallelCorpus, _corpusSpec.MaxCorpusCount);
			}
			throw new InvalidOperationException("An invalid alignment model type was specified.");
		}

		private ITrainer CreateThotAlignmentModelTrainer<TAlignModel>()
			where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			var directTrainer = new ThotWordAlignmentModelTrainer<TAlignModel>(_modelSpec.ModelPath + "_invswm",
				TokenProcessors.Lowercase, TokenProcessors.Lowercase, _corpusSpec.ParallelCorpus,
				_corpusSpec.MaxCorpusCount);
			var inverseTrainer = new ThotWordAlignmentModelTrainer<TAlignModel>(_modelSpec.ModelPath + "_swm",
				TokenProcessors.Lowercase, TokenProcessors.Lowercase, _corpusSpec.ParallelCorpus,
				_corpusSpec.MaxCorpusCount);
			return new SymmetrizedWordAlignmentModelTrainer(directTrainer, inverseTrainer);
		}
	}
}
