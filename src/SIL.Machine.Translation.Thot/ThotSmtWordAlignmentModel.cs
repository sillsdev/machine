using System;
using System.Collections.Generic;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotSmtWordAlignmentModel : DisposableBase, IWordAlignmentModel
	{
		private readonly ThotSmtModel _smtModel;
		private readonly ThotSmtEngine _smtEngine;
		private readonly bool _ownsSmtModel;

		public ThotSmtWordAlignmentModel(ThotWordAlignmentModelType wordAlignmentModelType, string cfgFileName)
			: this(new ThotSmtModel(wordAlignmentModelType, cfgFileName), true)
		{
		}

		public ThotSmtWordAlignmentModel(ThotWordAlignmentModelType wordAlignmentModelType,
			ThotSmtParameters parameters)
			: this(new ThotSmtModel(wordAlignmentModelType, parameters), true)
		{
		}

		public ThotSmtWordAlignmentModel(ThotSmtModel smtModel, bool ownsSmtModel = false)
		{
			_smtModel = smtModel;
			_smtEngine = _smtModel.CreateEngine();
			_ownsSmtModel = ownsSmtModel;
		}

		public IWordVocabulary SourceWords => _smtModel.DirectWordAlignmentModel.SourceWords;

		public IWordVocabulary TargetWords => _smtModel.DirectWordAlignmentModel.TargetWords;

		public IReadOnlySet<int> SpecialSymbolIndices => _smtModel.DirectWordAlignmentModel.SpecialSymbolIndices;

		public ITrainer CreateTrainer(ParallelTextCorpus corpus, ITokenProcessor sourcePreprocessor = null,
			ITokenProcessor targetPreprocessor = null, int maxCorpusCount = int.MaxValue)
		{
			CheckDisposed();

			return _smtModel.CreateTrainer(corpus, sourcePreprocessor, targetPreprocessor, maxCorpusCount);
		}

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			TranslationResult result = _smtEngine.GetBestPhraseAlignment(sourceSegment, targetSegment);
			return result.Alignment;
		}

		public IEnumerable<(string TargetWord, double Score)> GetTranslations(string sourceWord, double threshold = 0)
		{
			CheckDisposed();

			foreach ((string targetWord, double dirScore) in _smtModel.DirectWordAlignmentModel
				.GetTranslations(sourceWord))
			{
				double invScore = _smtModel.InverseWordAlignmentModel.GetTranslationScore(targetWord, sourceWord);
				double score = Math.Max(dirScore, invScore);
				if (score > threshold)
					yield return (targetWord, score);
			}
		}

		public IEnumerable<(int TargetWordIndex, double Score)> GetTranslations(int sourceWordIndex,
			double threshold = 0)
		{
			CheckDisposed();

			foreach ((int targetWordIndex, double dirScore) in _smtModel.DirectWordAlignmentModel
				.GetTranslations(sourceWordIndex))
			{
				double invScore = _smtModel.InverseWordAlignmentModel.GetTranslationScore(targetWordIndex,
					sourceWordIndex);
				double score = Math.Max(dirScore, invScore);
				if (score > threshold)
					yield return (targetWordIndex, score);
			}
		}

		public double GetTranslationScore(string sourceWord, string targetWord)
		{
			CheckDisposed();

			double dirScore = _smtModel.DirectWordAlignmentModel.GetTranslationScore(sourceWord, targetWord);
			double invScore = _smtModel.InverseWordAlignmentModel.GetTranslationScore(targetWord, sourceWord);
			return Math.Max(dirScore, invScore);
		}

		public double GetTranslationScore(int sourceWordIndex, int targetWordIndex)
		{
			CheckDisposed();

			double dirScore = _smtModel.DirectWordAlignmentModel.GetTranslationScore(sourceWordIndex, targetWordIndex);
			double invScore = _smtModel.InverseWordAlignmentModel.GetTranslationScore(targetWordIndex, sourceWordIndex);
			return Math.Max(dirScore, invScore);
		}

		public double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
			int prevTargetIndex, int targetIndex)
		{
			CheckDisposed();

			double dirScore = _smtModel.DirectWordAlignmentModel.GetAlignmentScore(sourceLen, prevSourceIndex,
				sourceIndex, targetLen, prevTargetIndex, targetIndex);
			double invScore = _smtModel.InverseWordAlignmentModel.GetAlignmentScore(targetLen, prevTargetIndex,
				targetIndex, sourceLen, prevSourceIndex, sourceIndex);
			return Math.Max(dirScore, invScore);
		}

		protected override void DisposeManagedResources()
		{
			_smtEngine.Dispose();
			if (_ownsSmtModel)
				_smtModel.Dispose();
		}
	}
}
