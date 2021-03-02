using System;
using System.Collections.Generic;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class SymmetrizedWordAlignmentModel : DisposableBase, IWordAlignmentModel
	{
		private readonly IWordAlignmentModel _directWordAlignmentModel;
		private readonly IWordAlignmentModel _inverseWordAlignmentModel;
		private readonly SymmetrizedWordAligner _aligner;

		public SymmetrizedWordAlignmentModel(IWordAlignmentModel directWordAlignmentModel,
			IWordAlignmentModel inverseWordAlignmentModel)
		{
			_directWordAlignmentModel = directWordAlignmentModel;
			_inverseWordAlignmentModel = inverseWordAlignmentModel;
			_aligner = new SymmetrizedWordAligner(DirectWordAlignmentModel, InverseWordAlignmentModel);
		}

		public SymmetrizationHeuristic Heuristic
		{
			get => _aligner.Heuristic;
			set => _aligner.Heuristic = value;
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

		public IReadOnlySet<int> SpecialSymbolIndices => _directWordAlignmentModel.SpecialSymbolIndices;

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			return _aligner.GetBestAlignment(sourceSegment, targetSegment);
		}

		public double GetTranslationScore(string sourceWord, string targetWord)
		{
			CheckDisposed();

			double score = _directWordAlignmentModel.GetTranslationScore(sourceWord, targetWord);
			double invScore = _inverseWordAlignmentModel.GetTranslationScore(targetWord, sourceWord);
			return Math.Max(score, invScore);
		}

		public double GetTranslationScore(int sourceWordIndex, int targetWordIndex)
		{
			CheckDisposed();

			double score = _directWordAlignmentModel.GetTranslationScore(sourceWordIndex, targetWordIndex);
			double invScore = _inverseWordAlignmentModel.GetTranslationScore(targetWordIndex, sourceWordIndex);
			return Math.Max(score, invScore);
		}

		public double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
			int prevTargetIndex, int targetIndex)
		{
			CheckDisposed();

			double score = _directWordAlignmentModel.GetAlignmentScore(sourceLen, prevSourceIndex, sourceIndex,
				targetLen, prevTargetIndex, targetIndex);
			double invScore = _inverseWordAlignmentModel.GetAlignmentScore(targetLen, prevTargetIndex, targetIndex,
				sourceLen, prevSourceIndex, sourceIndex);
			return Math.Max(score, invScore);
		}

		public ITrainer CreateTrainer(ITokenProcessor sourcePreprocessor, ITokenProcessor targetPreprocessor,
			ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue)
		{
			CheckDisposed();

			ITrainer directTrainer = _directWordAlignmentModel.CreateTrainer(sourcePreprocessor, targetPreprocessor,
				corpus, maxCorpusCount);
			ITrainer inverseTrainer = _inverseWordAlignmentModel.CreateTrainer(targetPreprocessor, sourcePreprocessor,
				corpus.Invert(), maxCorpusCount);

			return new SymmetrizedWordAlignmentModelTrainer(directTrainer, inverseTrainer);
		}

		protected override void DisposeManagedResources()
		{
			_directWordAlignmentModel.Dispose();
			_inverseWordAlignmentModel.Dispose();
		}
	}
}
