using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class SymmetrizedWordAlignmentModel : DisposableBase, IWordAlignmentModel
	{
		private readonly IWordAlignmentModel _directWordAlignmentModel;
		private readonly IWordAlignmentModel _inverseWordAlignmentModel;
		private readonly SymmetrizedSegmentAligner _aligner;

		public SymmetrizedWordAlignmentModel(IWordAlignmentModel directWordAlignmentModel,
			IWordAlignmentModel inverseWordAlignmentModel)
		{
			_directWordAlignmentModel = directWordAlignmentModel;
			_inverseWordAlignmentModel = inverseWordAlignmentModel;
			_aligner = new SymmetrizedSegmentAligner(DirectWordAlignmentModel, InverseWordAlignmentModel);
		}

		public IWordAlignmentModel DirectWordAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _directWordAlignmentModel;
			}
		}

		public IWordAlignmentModel InverseWordAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _inverseWordAlignmentModel;
			}
		}

		public IReadOnlyList<string> SourceWords
		{
			get
			{
				CheckDisposed();

				return _directWordAlignmentModel.SourceWords;
			}
		}

		public IReadOnlyList<string> TargetWords
		{
			get
			{
				CheckDisposed();

				return _directWordAlignmentModel.TargetWords;
			}
		}

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			return _aligner.GetBestAlignment(sourceSegment, targetSegment);
		}

		public double GetTranslationProbability(string sourceWord, string targetWord)
		{
			CheckDisposed();

			double transProb = _directWordAlignmentModel.GetTranslationProbability(sourceWord, targetWord);
			double invTransProb = _inverseWordAlignmentModel.GetTranslationProbability(targetWord, sourceWord);
			return Math.Max(transProb, invTransProb);
		}

		public double GetTranslationProbability(int sourceWordIndex, int targetWordIndex)
		{
			CheckDisposed();

			double transProb = _directWordAlignmentModel.GetTranslationProbability(sourceWordIndex, targetWordIndex);
			double invTransProb = _inverseWordAlignmentModel.GetTranslationProbability(targetWordIndex,
				sourceWordIndex);
			return Math.Max(transProb, invTransProb);
		}

		public double GetAlignmentProbability(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
			int prevTargetIndex, int targetIndex)
		{
			double alignProb = _directWordAlignmentModel.GetAlignmentProbability(sourceLen, prevSourceIndex,
				sourceIndex, targetLen, prevTargetIndex, targetIndex);
			double invAlignProb = _inverseWordAlignmentModel.GetAlignmentProbability(targetLen, prevTargetIndex,
				targetIndex, sourceLen, prevSourceIndex, sourceIndex);
			return Math.Max(alignProb, invAlignProb);
		}

		public ITrainer CreateTrainer(ITokenProcessor sourcePreprocessor, ITextCorpus sourceCorpus,
			ITokenProcessor targetPreprocessor, ITextCorpus targetCorpus, ITextAlignmentCorpus alignmentCorpus = null)
		{
			CheckDisposed();

			ITrainer directTrainer = _directWordAlignmentModel.CreateTrainer(sourcePreprocessor, sourceCorpus,
				targetPreprocessor, targetCorpus, alignmentCorpus);
			ITrainer inverseTrainer = _inverseWordAlignmentModel.CreateTrainer(targetPreprocessor, targetCorpus,
				sourcePreprocessor, sourceCorpus, alignmentCorpus?.Invert());

			return new Trainer(directTrainer, inverseTrainer);
		}

		public void Save()
		{
			CheckDisposed();

			_directWordAlignmentModel.Save();
			_inverseWordAlignmentModel.Save();
		}

		public async Task SaveAsync()
		{
			CheckDisposed();

			await _directWordAlignmentModel.SaveAsync();
			await _inverseWordAlignmentModel.SaveAsync();
		}

		protected override void DisposeManagedResources()
		{
			_directWordAlignmentModel.Dispose();
			_inverseWordAlignmentModel.Dispose();
		}

		private class Trainer : DisposableBase, ITrainer
		{
			private readonly ITrainer _directTrainer;
			private readonly ITrainer _inverseTrainer;

			public Trainer(ITrainer directTrainer, ITrainer inverseTrainer)
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
}
