using System;
using System.Collections.Generic;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
	public class ThotSmtWordAlignmentModel : ThotSmtWordAlignmentModel<HmmWordAlignmentModel>
	{
		public ThotSmtWordAlignmentModel(string cfgFileName)
			: base(new ThotSmtModel(cfgFileName))
		{
		}

		public ThotSmtWordAlignmentModel(ThotSmtParameters parameters)
			: base(new ThotSmtModel(parameters))
		{
		}

		public ThotSmtWordAlignmentModel(ThotSmtModel smtModel)
			: base(smtModel)
		{
		}
	}


	public class ThotSmtWordAlignmentModel<TAlignModel> : DisposableBase, IWordAlignmentModel
		where TAlignModel : ThotWordAlignmentModelBase<TAlignModel>, new()
	{
		private readonly ThotSmtModel<TAlignModel> _smtModel;
		private readonly ThotSmtEngine _smtEngine;
		private readonly bool _ownsSmtModel;

		public ThotSmtWordAlignmentModel(string cfgFileName)
			: this(new ThotSmtModel<TAlignModel>(cfgFileName), true)
		{
		}

		public ThotSmtWordAlignmentModel(ThotSmtParameters parameters)
			: this(new ThotSmtModel<TAlignModel>(parameters), true)
		{
		}

		public ThotSmtWordAlignmentModel(ThotSmtModel<TAlignModel> smtModel, bool ownsSmtModel = false)
		{
			_smtModel = smtModel;
			_smtEngine = _smtModel.CreateEngine();
			_ownsSmtModel = ownsSmtModel;
		}

		public IReadOnlyList<string> SourceWords => _smtModel.DirectWordAlignmentModel.SourceWords;

		public IReadOnlyList<string> TargetWords => _smtModel.DirectWordAlignmentModel.TargetWords;

		public IReadOnlySet<int> SpecialSymbolIndices => _smtModel.DirectWordAlignmentModel.SpecialSymbolIndices;

		public ITrainer CreateTrainer(ITokenProcessor sourcePreprocessor, ITokenProcessor targetPreprocessor,
			ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue)
		{
			CheckDisposed();

			return _smtModel.CreateTrainer(sourcePreprocessor, targetPreprocessor, corpus, maxCorpusCount);
		}

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			TranslationResult result = _smtEngine.GetBestPhraseAlignment(sourceSegment, targetSegment);
			return result.Alignment;
		}

		public double GetTranslationScore(string sourceWord, string targetWord)
		{
			CheckDisposed();

			double score = _smtModel.DirectWordAlignmentModel.GetTranslationScore(sourceWord, targetWord);
			double invScore = _smtModel.InverseWordAlignmentModel.GetTranslationScore(targetWord, sourceWord);
			return Math.Max(score, invScore);
		}

		public double GetTranslationScore(int sourceWordIndex, int targetWordIndex)
		{
			CheckDisposed();

			double score = _smtModel.DirectWordAlignmentModel.GetTranslationScore(sourceWordIndex, targetWordIndex);
			double invScore = _smtModel.InverseWordAlignmentModel.GetTranslationScore(targetWordIndex, sourceWordIndex);
			return Math.Max(score, invScore);
		}

		public double GetAlignmentScore(int sourceLen, int prevSourceIndex, int sourceIndex, int targetLen,
			int prevTargetIndex, int targetIndex)
		{
			CheckDisposed();

			double score = _smtModel.DirectWordAlignmentModel.GetAlignmentScore(sourceLen, prevSourceIndex,
				sourceIndex, targetLen, prevTargetIndex, targetIndex);
			double invScore = _smtModel.InverseWordAlignmentModel.GetAlignmentScore(targetLen, prevTargetIndex,
				targetIndex, sourceLen, prevSourceIndex, sourceIndex);
			return Math.Max(score, invScore);
		}

		protected override void DisposeManagedResources()
		{
			_smtEngine.Dispose();
			if (_ownsSmtModel)
				_smtModel.Dispose();
		}
	}
}
