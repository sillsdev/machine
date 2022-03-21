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

		public IWordVocabulary SourceWords
		{
			get
			{
				CheckDisposed();

				return _directWordAlignmentModel.SourceWords;
			}
		}

		public IWordVocabulary TargetWords
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

		public IEnumerable<(string TargetWord, double Score)> GetTranslations(string sourceWord, double threshold = 0)
		{
			CheckDisposed();

			foreach ((string targetWord, double dirScore) in _directWordAlignmentModel.GetTranslations(sourceWord))
			{
				double invScore = _inverseWordAlignmentModel.GetTranslationScore(targetWord, sourceWord);
				double score = Math.Max(dirScore, invScore);
				if (score > threshold)
					yield return (targetWord, score);
			}
		}

		public IEnumerable<(int TargetWordIndex, double Score)> GetTranslations(int sourceWordIndex,
			double threshold = 0)
		{
			CheckDisposed();

			foreach ((int targetWordIndex, double dirScore) in _directWordAlignmentModel.GetTranslations(
				sourceWordIndex))
			{
				double invScore = _inverseWordAlignmentModel.GetTranslationScore(targetWordIndex, sourceWordIndex);
				double score = Math.Max(dirScore, invScore);
				if (score > threshold)
					yield return (targetWordIndex, score);
			}
		}

		public double GetTranslationScore(string sourceWord, string targetWord)
		{
			CheckDisposed();

			double dirScore = _directWordAlignmentModel.GetTranslationScore(sourceWord, targetWord);
			double invScore = _inverseWordAlignmentModel.GetTranslationScore(targetWord, sourceWord);
			return Math.Max(dirScore, invScore);
		}

		public double GetTranslationScore(int sourceWordIndex, int targetWordIndex)
		{
			CheckDisposed();

			double dirScore = _directWordAlignmentModel.GetTranslationScore(sourceWordIndex, targetWordIndex);
			double invScore = _inverseWordAlignmentModel.GetTranslationScore(targetWordIndex, sourceWordIndex);
			return Math.Max(dirScore, invScore);
		}

		public double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
			int prevTargetIndex, int targetIndex)
		{
			CheckDisposed();

			double dirScore = _directWordAlignmentModel.GetAlignmentScore(sourceLen, prevSourceIndex, sourceIndex,
				targetLen, prevTargetIndex, targetIndex);
			double invScore = _inverseWordAlignmentModel.GetAlignmentScore(targetLen, prevTargetIndex, targetIndex,
				sourceLen, prevSourceIndex, sourceIndex);
			return Math.Max(dirScore, invScore);
		}

		public ITrainer CreateTrainer(IParallelTextCorpusView corpus)
		{
			CheckDisposed();

			ITrainer directTrainer = _directWordAlignmentModel.CreateTrainer(corpus);
			ITrainer inverseTrainer = _inverseWordAlignmentModel.CreateTrainer(corpus.Invert());

			return new SymmetrizedWordAlignmentModelTrainer(directTrainer, inverseTrainer);
		}

		protected override void DisposeManagedResources()
		{
			_directWordAlignmentModel.Dispose();
			_inverseWordAlignmentModel.Dispose();
		}
	}
}
