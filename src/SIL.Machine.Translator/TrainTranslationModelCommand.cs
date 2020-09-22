using System;
using System.Diagnostics;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine.Translation
{
	public class TrainTranslationModelCommand : TranslationModelCommandBase
	{
		private readonly CommandOption _quietOption;

		public TrainTranslationModelCommand()
			: base(supportAlignmentsCorpus: true)
		{
			Name = "translation";
			Description = "Trains a machine translation model from a parallel corpus.";

			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (!Directory.Exists(ModelDirectory))
				Directory.CreateDirectory(ModelDirectory);

			TrainTranslationModel();

			return 0;
		}

		private void TrainTranslationModel()
		{
			if (!File.Exists(ModelConfigFileName))
			{
				string defaultConfigFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data",
					"default-smt.cfg");
				File.Copy(defaultConfigFileName, ModelConfigFileName);
			}

			using (ITranslationModelTrainer trainer = new ThotSmtModelTrainer(ModelConfigFileName,
				TokenProcessors.Lowercase, TokenProcessors.Lowercase, ParallelCorpus, MaxParallelCorpusCount))
			{
				Stopwatch watch = Stopwatch.StartNew();
				if (!_quietOption.HasValue())
					Out.Write("Training... ");
				using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
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
	}
}
