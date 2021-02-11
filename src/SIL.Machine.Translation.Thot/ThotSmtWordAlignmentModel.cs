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

		public string NullWord => _smtModel.DirectWordAlignmentModel.NullWord;
		public int NullIndex => _smtModel.DirectWordAlignmentModel.NullIndex;

		public bool IsProbabilityDistributionNormalized => false;

		public ITrainer CreateTrainer(ITokenProcessor sourcePreprocessor, ITokenProcessor targetPreprocessor,
			ParallelTextCorpus corpus, int maxCorpusCount = int.MaxValue)
		{
			return _smtModel.CreateTrainer(sourcePreprocessor, targetPreprocessor, corpus, maxCorpusCount);
		}

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			TranslationResult result = _smtEngine.GetBestPhraseAlignment(sourceSegment, targetSegment);
			return result.Alignment;
		}

		public double GetTranslationProbability(string sourceWord, string targetWord)
		{
			double prob = _smtModel.DirectWordAlignmentModel.GetTranslationProbability(sourceWord, targetWord);
			double invProb = _smtModel.InverseWordAlignmentModel.GetTranslationProbability(targetWord,
				sourceWord);
			return Math.Max(prob, invProb);
		}

		public double GetTranslationProbability(int sourceWordIndex, int targetWordIndex)
		{
			return GetTranslationProbability(SourceWords[sourceWordIndex], TargetWords[targetWordIndex]);
		}

		protected override void DisposeManagedResources()
		{
			_smtEngine.Dispose();
			if (_ownsSmtModel)
				_smtModel.Dispose();
		}
	}
}
