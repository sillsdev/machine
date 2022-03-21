using System;
using System.Linq;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.Machine.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class UnigramTruecaserTrainer : DisposableBase, ITrainer
	{
		private readonly string _modelPath;
		private readonly ITextCorpus _corpus;

		public UnigramTruecaserTrainer(string modelPath, ITextCorpus corpus)
		{
			_modelPath = modelPath;
			_corpus = corpus;
			NewTruecaser = new UnigramTruecaser();
		}

		public UnigramTruecaserTrainer(ITextCorpus corpus)
			: this(null, corpus)
		{
		}

		public TrainStats Stats { get; } = new TrainStats();

		protected UnigramTruecaser NewTruecaser { get; }

		public void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null)
		{
			int stepCount = 0;
			if (progress != null)
				stepCount = _corpus.Count();
			int currentStep = 0;
			foreach (TextCorpusRow segment in _corpus.GetRows())
			{
				checkCanceled?.Invoke();
				NewTruecaser.TrainSegment(segment);
				currentStep++;
				if (progress != null)
					progress.Report(new ProgressStatus(currentStep, stepCount));
			}
			Stats.TrainedSegmentCount = currentStep;
		}

		public virtual void Save()
		{
			if (_modelPath != null)
				NewTruecaser.Save(_modelPath);
		}

		public virtual async Task SaveAsync()
		{
			if (_modelPath != null)
				await NewTruecaser.SaveAsync(_modelPath);
		}
	}
}
