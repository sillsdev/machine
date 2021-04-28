using System;
using System.Threading.Tasks;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class SymmetrizedWordAlignmentModelTrainer : DisposableBase, ITrainer
	{
		private readonly ITrainer _directTrainer;
		private readonly ITrainer _inverseTrainer;

		public TrainStats Stats => _directTrainer.Stats;

		public SymmetrizedWordAlignmentModelTrainer(ITrainer directTrainer, ITrainer inverseTrainer)
		{
			_directTrainer = directTrainer;
			_inverseTrainer = inverseTrainer;
		}

		public void Train(IProgress<ProgressStatus> progress = null, Action checkCanceled = null)
		{
			CheckDisposed();

			var reporter = new PhasedProgressReporter(progress,
				new Phase("Training direct alignment model"),
				new Phase("Training inverse alignment model"));

			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				_directTrainer.Train(phaseProgress, checkCanceled);
			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				_inverseTrainer.Train(phaseProgress, checkCanceled);
		}

		public async Task SaveAsync()
		{
			CheckDisposed();

			await _directTrainer.SaveAsync();
			await _inverseTrainer.SaveAsync();
		}

		public void Save()
		{
			CheckDisposed();

			_directTrainer.Save();
			_inverseTrainer.Save();
		}

		protected override void DisposeManagedResources()
		{
			_directTrainer.Dispose();
			_inverseTrainer.Dispose();
		}
	}
}
