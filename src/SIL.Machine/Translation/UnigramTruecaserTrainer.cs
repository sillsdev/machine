using System;
using System.Collections.Generic;
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
		private readonly IEnumerable<TextRow> _corpus;

		public UnigramTruecaserTrainer(string modelPath, IEnumerable<TextRow> corpus)
		{
			_modelPath = modelPath;
			_corpus = corpus;
			NewTruecaser = new UnigramTruecaser();
		}

		public UnigramTruecaserTrainer(IEnumerable<TextRow> corpus)
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
			foreach (TextRow row in _corpus)
			{
				checkCanceled?.Invoke();
				NewTruecaser.TrainSegment(row);
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
