using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Translation;
using SIL.Machine.Translation.Thot;

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
			if (_modelSpec.ModelFactory != null)
			{
				return _modelSpec.ModelFactory.CreateTrainer(_modelSpec.ModelPath, TokenProcessors.Lowercase,
					TokenProcessors.Lowercase, _corpusSpec.ParallelCorpus, _corpusSpec.MaxCorpusCount);
			}

			switch (_modelSpec.ModelType)
			{
				case "hmm":
					return CreateThotAlignmentModelTrainer<HmmThotWordAlignmentModel>();
				case "ibm1":
					return CreateThotAlignmentModelTrainer<Ibm1ThotWordAlignmentModel>();
				case "ibm2":
					return CreateThotAlignmentModelTrainer<Ibm2ThotWordAlignmentModel>();
				case "smt":
					string modelCfgFileName = ToolHelpers.GetTranslationModelConfigFileName(_modelSpec.ModelPath);
					string modelDir = Path.GetDirectoryName(modelCfgFileName);
					if (!Directory.Exists(modelDir))
						Directory.CreateDirectory(modelDir);
					if (!File.Exists(modelCfgFileName))
					{
						string defaultConfigFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data",
							"default-smt.cfg");
						File.Copy(defaultConfigFileName, modelCfgFileName);
					}
					return new ThotSmtModelTrainer(modelCfgFileName, TokenProcessors.Lowercase,
						TokenProcessors.Lowercase, _corpusSpec.ParallelCorpus, _corpusSpec.MaxCorpusCount);
			}
			throw new InvalidOperationException("An invalid alignment model type was specified.");
		}

		private ITrainer CreateThotAlignmentModelTrainer<TAlignModel>()
			where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			string modelPath = _modelSpec.ModelPath;
			if (ToolHelpers.IsDirectoryPath(modelPath))
				modelPath = Path.Combine(modelPath, "src_trg");
			string modelDir = Path.GetDirectoryName(modelPath);
			if (!Directory.Exists(modelDir))
				Directory.CreateDirectory(modelDir);
			var directTrainer = new ThotWordAlignmentModelTrainer<TAlignModel>(modelPath + "_invswm",
				TokenProcessors.Lowercase, TokenProcessors.Lowercase, _corpusSpec.ParallelCorpus,
				_corpusSpec.MaxCorpusCount);
			var inverseTrainer = new ThotWordAlignmentModelTrainer<TAlignModel>(modelPath + "_swm",
				TokenProcessors.Lowercase, TokenProcessors.Lowercase, _corpusSpec.ParallelCorpus.Invert(),
				_corpusSpec.MaxCorpusCount);
			return new SymmetrizedWordAlignmentModelTrainer(directTrainer, inverseTrainer);
		}
	}
}
