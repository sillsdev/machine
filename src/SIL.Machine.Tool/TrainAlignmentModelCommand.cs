using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.Utils;

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

		protected override async Task<int> ExecuteCommandAsync(CancellationToken ct)
		{
			int code = await base.ExecuteCommandAsync(ct);
			if (code != 0)
				return code;

			if (_trainParamsOption.Values.Any(kvp => !kvp.Contains("=")))
			{
				Out.WriteLine("The training parameters are not formatted correctly.");
				return 1;
			}

			Dictionary<string, string> parameters = _trainParamsOption.Values.Select(kvp => kvp.Split("="))
				.ToDictionary(kvp => kvp[0].ToLowerInvariant(), kvp => kvp[1]);

			int trainedSegmentCount = 0;
			if (!_quietOption.HasValue())
				Out.Write("Training... ");
			var watch = Stopwatch.StartNew();
			using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			{
				Phase[] phases;
				if (_modelSpec.IsSymmetric)
				{
					phases = new Phase[]
					{
						new Phase("Training model", 0.96),
						new Phase("Saving model", 0.04)
					};
				}
				else
				{
					phases = new Phase[]
					{
						new Phase("Training direct model", 0.48),
						new Phase("Saving direct model", 0.02),
						new Phase("Training inverse model", 0.48),
						new Phase("Saving inverse model", 0.02)
					};
				}
				var reporter = new PhasedProgressReporter(progress, phases);

				IParallelTextCorpusView corpus = _preprocessSpec.Preprocess(_corpusSpec.ParallelCorpus);
				using (ITrainer trainer = _modelSpec.CreateAlignmentModelTrainer(corpus, _corpusSpec.MaxCorpusCount,
					parameters, direct: true))
				{
					using (PhaseProgress phaseProgress = reporter.StartNextPhase())
						trainer.Train(phaseProgress);
					using (PhaseProgress phaseProgress = reporter.StartNextPhase())
						trainer.Save();
					trainedSegmentCount = trainer.Stats.TrainedSegmentCount;
				}

				if (!_modelSpec.IsSymmetric)
				{
					using ITrainer trainer = _modelSpec.CreateAlignmentModelTrainer(corpus, _corpusSpec.MaxCorpusCount,
						parameters, direct: false);
					using (PhaseProgress phaseProgress = reporter.StartNextPhase())
						trainer.Train(phaseProgress);
					using (PhaseProgress phaseProgress = reporter.StartNextPhase())
						trainer.Save();

					trainedSegmentCount = Math.Max(trainedSegmentCount, trainer.Stats.TrainedSegmentCount);
				}
			}
			if (!_quietOption.HasValue())
				Out.WriteLine("done.");
			watch.Stop();

			Out.WriteLine($"Execution time: {watch.Elapsed:c}");
			Out.WriteLine($"# of Segments Trained: {trainedSegmentCount}");

			return 0;
		}
	}
}
