using System;
using System.Collections.Generic;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotSymmetrizedWordAlignmentModel : DisposableBase, ISegmentAligner
	{
		private readonly ThotWordAlignmentModel _directWordAlignmentModel;
		private readonly ThotWordAlignmentModel _inverseWordAlignmentModel;
		private readonly SymmetrizedSegmentAligner _aligner;

		public ThotSymmetrizedWordAlignmentModel()
			: this(new ThotWordAlignmentModel(), new ThotWordAlignmentModel())
		{
		}

		public ThotSymmetrizedWordAlignmentModel(string directPrefFileName, string inversePrefFileName,
			bool createNew = false)
			: this(new ThotWordAlignmentModel(directPrefFileName, createNew),
				  new ThotWordAlignmentModel(inversePrefFileName, createNew))
		{
		}

		private ThotSymmetrizedWordAlignmentModel(ThotWordAlignmentModel directModel,
			ThotWordAlignmentModel inverseModel)
		{
			_directWordAlignmentModel = directModel;
			_inverseWordAlignmentModel = inverseModel;
			_aligner = new SymmetrizedSegmentAligner(DirectWordAlignmentModel, InverseWordAlignmentModel);
		}

		public ThotWordAlignmentModel DirectWordAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _directWordAlignmentModel;
			}
		}

		public ThotWordAlignmentModel InverseWordAlignmentModel
		{
			get
			{
				CheckDisposed();

				return _inverseWordAlignmentModel;
			}
		}

		public IReadOnlyList<string> SourceWords => _directWordAlignmentModel.SourceWords;
		public IReadOnlyList<string> TargetWords => _directWordAlignmentModel.TargetWords;

		public void AddSegmentPairs(ParallelTextCorpus corpus, Func<string, string> preprocessor = null,
			int maxCount = int.MaxValue)
		{
			CheckDisposed();

			_directWordAlignmentModel.AddSegmentPairs(corpus, preprocessor, maxCount);
			_inverseWordAlignmentModel.AddSegmentPairs(corpus.Invert(), preprocessor, maxCount);
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

			TrainAlignmentModel(_directWordAlignmentModel, progress, iterCount, 0);
			TrainAlignmentModel(_inverseWordAlignmentModel, progress, iterCount, iterCount);
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
			double invTransProb = _inverseWordAlignmentModel.GetTranslationProbability(targetWordIndex, sourceWordIndex);
			return Math.Max(transProb, invTransProb);
		}

		public double GetAlignmentProbability(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
			int prevTargetIndex, int targetIndex)
		{
			CheckDisposed();

			double alignProb = _directWordAlignmentModel.GetAlignmentProbability(sourceLen, prevSourceIndex,
				sourceIndex);
			double invAlignProb = _inverseWordAlignmentModel.GetAlignmentProbability(targetLen, prevTargetIndex,
				targetIndex);
			return Math.Max(alignProb, invAlignProb);
		}

		public IDictionary<string, IDictionary<string, double>> GetTranslationTable(double threshold = 0)
		{
			CheckDisposed();

			var results = new Dictionary<string, IDictionary<string, double>>();
			for (int i = 0; i < SourceWords.Count; i++)
			{
				var row = new Dictionary<string, double>();
				for (int j = 0; j < TargetWords.Count; j++)
				{
					double prob = GetTranslationProbability(i, j);
					if (prob > threshold)
						row[TargetWords[j]] = prob;
				}
				results[SourceWords[i]] = row;
			}
			return results;
		}

		public void Save()
		{
			CheckDisposed();

			_directWordAlignmentModel.Save();
			_inverseWordAlignmentModel.Save();
		}

		private void TrainAlignmentModel(ThotWordAlignmentModel model, IProgress<double> progress, int iterCount,
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
			_directWordAlignmentModel.Dispose();
			_inverseWordAlignmentModel.Dispose();
		}
	}
}
