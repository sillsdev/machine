using System;
using System.Collections.Generic;
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

		public void AddSegmentPair(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			_directWordAlignmentModel.AddSegmentPair(sourceSegment, targetSegment);
			_inverseWordAlignmentModel.AddSegmentPair(targetSegment, sourceSegment);
		}

		public void Train(IProgress<ProgressStatus> progress = null)
		{
			CheckDisposed();

			var reporter = new PhasedProgressReporter(progress,
				new Phase("Training direct alignment model"),
				new Phase("Training inverse alignment model"));

			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				_directWordAlignmentModel.Train(phaseProgress);
			using (PhaseProgress phaseProgress = reporter.StartNextPhase())
				_inverseWordAlignmentModel.Train(phaseProgress);
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

		public void Save()
		{
			CheckDisposed();

			_directWordAlignmentModel.Save();
			_inverseWordAlignmentModel.Save();
		}

		protected override void DisposeManagedResources()
		{
			_directWordAlignmentModel.Dispose();
			_inverseWordAlignmentModel.Dispose();
		}
	}
}
