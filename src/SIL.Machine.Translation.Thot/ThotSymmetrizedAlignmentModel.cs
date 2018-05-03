using System;
using System.Collections.Generic;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotSymmetrizedAlignmentModel : DisposableBase, ISegmentAligner
	{
		private readonly ThotAlignmentModel _directAlignmentModel;
		private readonly ThotAlignmentModel _inverseAlignmentModel;
		private readonly SymmetrizedSegmentAligner _aligner;

		public ThotSymmetrizedAlignmentModel()
			: this(new ThotAlignmentModel(), new ThotAlignmentModel())
		{
		}

		public ThotSymmetrizedAlignmentModel(string directPrefFileName, string inversePrefFileName,
			bool createNew = false)
			: this(new ThotAlignmentModel(directPrefFileName, createNew),
				  new ThotAlignmentModel(inversePrefFileName, createNew))
		{
		}

		private ThotSymmetrizedAlignmentModel(ThotAlignmentModel directModel, ThotAlignmentModel inverseModel)
		{
			_directAlignmentModel = directModel;
			_inverseAlignmentModel = inverseModel;
			_aligner = new SymmetrizedSegmentAligner(DirectAlignmentModel, InverseAlignmentModel);
		}

		public ThotAlignmentModel DirectAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _directAlignmentModel;
			}
		}

		public ThotAlignmentModel InverseAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _inverseAlignmentModel;
			}
		}

		public void AddSegmentPairs(ParallelTextCorpus corpus, Func<string, string> preprocessor = null,
			int maxCount = int.MaxValue)
		{
			CheckDisposed();

			_directAlignmentModel.AddSegmentPairs(corpus, preprocessor, maxCount);
			_inverseAlignmentModel.AddSegmentPairs(corpus.Invert(), preprocessor, maxCount);
		}

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment, WordAlignmentMatrix hintMatrix = null)
		{
			CheckDisposed();

			return _aligner.GetBestAlignment(sourceSegment, targetSegment, hintMatrix);
		}

		public void Train(IProgress<double> progress = null, int iterCount = 5)
		{
			CheckDisposed();

			TrainAlignmentModel(_directAlignmentModel, progress, iterCount, 0);
			TrainAlignmentModel(_inverseAlignmentModel, progress, iterCount, iterCount);
		}

		public double GetTranslationProbability(string sourceWord, string targetWord)
		{
			CheckDisposed();

			double transProb = _directAlignmentModel.GetTranslationProbability(sourceWord, targetWord);
			double invTransProb = _inverseAlignmentModel.GetTranslationProbability(targetWord, sourceWord);
			return Math.Max(transProb, invTransProb);
		}

		public double GetAlignmentProbability(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
			int prevTargetIndex, int targetIndex)
		{
			CheckDisposed();

			double alignProb = _directAlignmentModel.GetAlignmentProbability(sourceLen, prevSourceIndex, sourceIndex);
			double invAlignProb = _inverseAlignmentModel.GetAlignmentProbability(targetLen, prevTargetIndex,
				targetIndex);
			return Math.Max(alignProb, invAlignProb);
		}

		public IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> GetTranslationTable(double threshold = 0)
		{
			CheckDisposed();

			var results = new Dictionary<string, IReadOnlyDictionary<string, double>>();
			foreach (string sourceWord in _directAlignmentModel.SourceWords)
			{
				var targetWords = new Dictionary<string, double>();
				foreach (string targetWord in _directAlignmentModel.TargetWords)
				{
					double prob = GetTranslationProbability(sourceWord, targetWord);
					if (prob > threshold)
						targetWords[targetWord] = prob;
				}
				results[sourceWord] = targetWords;
			}
			return results;
		}

		public void Save()
		{
			CheckDisposed();

			_directAlignmentModel.Save();
			_inverseAlignmentModel.Save();
		}

		private void TrainAlignmentModel(ThotAlignmentModel model, IProgress<double> progress, int iterCount,
			int startStep)
		{
			for (int i = 0; i < iterCount; i++)
			{
				model.Train(1);
				progress?.Report((double)(startStep + i + 1) / (iterCount * 2));
			}
		}

		protected override void DisposeManagedResources()
		{
			_directAlignmentModel.Dispose();
			_inverseAlignmentModel.Dispose();
		}
	}
}
