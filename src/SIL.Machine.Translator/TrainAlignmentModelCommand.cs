using System;
using McMaster.Extensions.CommandLineUtils;
using SIL.Machine.Corpora;
using SIL.Machine.Translation.Paratext;
using SIL.Machine.Translation.Thot;

namespace SIL.Machine.Translation
{
	public class TrainAlignmentModelCommand : AlignmentModelCommandBase
	{
		private readonly CommandOption _quietOption;

		public TrainAlignmentModelCommand()
			: base(supportAlignmentsCorpus: true)
		{
			Name = "alignment";
			Description = "Trains a word alignment model from a parallel corpus.";

			_quietOption = Option("-q|--quiet", "Only display results.", CommandOptionType.NoValue);
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			TrainAlignmentModel();

			return 0;
		}

		private void TrainAlignmentModel()
		{
			int parallelCorpusCount = GetParallelCorpusCount();

			if (!_quietOption.HasValue())
				Out.Write("Training... ");
			using (ConsoleProgressBar progress = _quietOption.HasValue() ? null : new ConsoleProgressBar(Out))
			using (ITrainer trainer = CreateAlignmentModelTrainer())
			{
				trainer.Train(progress);
				trainer.Save();
			}
			if (!_quietOption.HasValue())
				Out.WriteLine("done.");

			Out.WriteLine($"# of Segments Trained: {parallelCorpusCount}");
		}

		private ITrainer CreateAlignmentModelTrainer()
		{
			switch (ModelType)
			{
				case "hmm":
					return CreateThotAlignmentModelTrainer<HmmThotWordAlignmentModel>(ModelPath, ParallelCorpus,
						MaxParallelCorpusCount);
				case "ibm1":
					return CreateThotAlignmentModelTrainer<Ibm1ThotWordAlignmentModel>(ModelPath, ParallelCorpus,
						MaxParallelCorpusCount);
				case "ibm2":
					return CreateThotAlignmentModelTrainer<Ibm2ThotWordAlignmentModel>(ModelPath, ParallelCorpus,
						MaxParallelCorpusCount);
				case "pt":
					return new ParatextWordAlignmentModelTrainer(ModelPath, TokenProcessors.Lowercase,
						TokenProcessors.Lowercase, ParallelCorpus, MaxParallelCorpusCount);
			}
			throw new InvalidOperationException("An invalid alignment model type was specified.");
		}

		private static ITrainer CreateThotAlignmentModelTrainer<TAlignModel>(string path, ParallelTextCorpus corpus,
			int maxCorpusCount) where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
		{
			var directTrainer = new ThotWordAlignmentModelTrainer<TAlignModel>(path + "_invswm",
				TokenProcessors.Lowercase, TokenProcessors.Lowercase, corpus, maxCorpusCount);
			var inverseTrainer = new ThotWordAlignmentModelTrainer<TAlignModel>(path + "_swm",
				TokenProcessors.Lowercase, TokenProcessors.Lowercase, corpus, maxCorpusCount);
			return new SymmetrizedWordAlignmentModelTrainer(directTrainer, inverseTrainer);
		}
	}
}
