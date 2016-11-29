using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class HybridTranslationSession : DisposableBase, IInteractiveTranslationSession
	{
		private readonly HybridTranslationEngine _engine;
		private readonly IInteractiveSmtEngine _smtEngine;
		private readonly IInteractiveTranslationSession _smtSession;
		private readonly ITranslationEngine _ruleEngine;
		private TranslationResult _currentResult;
		private TranslationResult _ruleResult;

		internal HybridTranslationSession(HybridTranslationEngine engine, IInteractiveSmtEngine smtEngine, IInteractiveTranslationSession smtSession,
			ITranslationEngine ruleEngine)
		{
			_engine = engine;
			_smtEngine = smtEngine;
			_smtSession = smtSession;
			_ruleEngine = ruleEngine;
		}

		public ReadOnlyList<string> SourceSegment
		{
			get
			{
				CheckDisposed();
				return _smtSession.SourceSegment;
			}
		}

		public ReadOnlyList<string> Prefix
		{
			get
			{
				CheckDisposed();
				return _smtSession.Prefix;
			}
		}

		public bool IsLastWordComplete
		{
			get
			{
				CheckDisposed();
				return _smtSession.IsLastWordComplete;
			}
		}

		public TranslationResult CurrenTranslationResult
		{
			get
			{
				CheckDisposed();
				return _currentResult;
			}
		}

		public TranslationResult TranslateInteractively(IEnumerable<string> sourceSegment)
		{
			CheckDisposed();

			_ruleResult = _ruleEngine.Translate(sourceSegment);
			TranslationResult smtResult = _smtSession.TranslateInteractively(_ruleResult.SourceSegment);
			_currentResult = smtResult.Merge(0, HybridTranslationEngine.RuleEngineThreshold, _ruleResult);
			return _currentResult;
		}

		public TranslationResult TranslateInteractively(string sourceSegment)
		{
			CheckDisposed();
			_engine.CheckSourceTokenizer();

			return TranslateInteractively(HybridTranslationEngine.Preprocess(_engine.SourcePreprocessor, _engine.SourceTokenizer, sourceSegment));
		}

		public TranslationResult SetPrefix(IEnumerable<string> prefix, bool isLastWordComplete)
		{
			CheckDisposed();

			TranslationResult smtResult = _smtSession.SetPrefix(prefix, isLastWordComplete);
			int prefixCount = _smtSession.Prefix.Count;
			if (!_smtSession.IsLastWordComplete)
				prefixCount--;
			_currentResult = smtResult.Merge(prefixCount, HybridTranslationEngine.RuleEngineThreshold, _ruleResult);
			return _currentResult;
		}

		public TranslationResult SetPrefix(string prefix, bool isLastWordComplete)
		{
			CheckDisposed();
			_engine.CheckTargetTokenizer();

			return SetPrefix(HybridTranslationEngine.Preprocess(_engine.TargetPreprocessor, _engine.TargetTokenizer, prefix), isLastWordComplete);
		}

		public TranslationResult AddToPrefix(IEnumerable<string> addition, bool isLastWordComplete)
		{
			CheckDisposed();

			TranslationResult smtResult = _smtSession.AddToPrefix(addition, isLastWordComplete);
			int prefixCount = _smtSession.Prefix.Count;
			if (!_smtSession.IsLastWordComplete)
				prefixCount--;
			_currentResult = smtResult.Merge(prefixCount, HybridTranslationEngine.RuleEngineThreshold, _ruleResult);
			return _currentResult;
		}

		public TranslationResult AddToPrefix(string addition, bool isLastWordComplete)
		{
			CheckDisposed();
			_engine.CheckTargetTokenizer();

			return AddToPrefix(HybridTranslationEngine.Preprocess(_engine.TargetPreprocessor, _engine.TargetTokenizer, addition), isLastWordComplete);
		}

		public void Reset()
		{
			CheckDisposed();

			_currentResult = null;
			_ruleResult = null;
			_smtSession.Reset();
		}

		public void Approve()
		{
			CheckDisposed();

			TranslationResult smtResult = _smtEngine.GetBestPhraseAlignment(SourceSegment, Prefix);
			TranslationResult hybridResult = smtResult.Merge(Prefix.Count, HybridTranslationEngine.RuleEngineThreshold, _ruleResult);

			var matrix = new WordAlignmentMatrix(SourceSegment.Count, Prefix.Count, AlignmentType.Unknown);
			var iAligned = new HashSet<int>();
			for (int j = 0; j < Prefix.Count; j++)
			{
				bool jAligned = false;
				foreach (AlignedWordPair wp in hybridResult.GetTargetWordPairs(j))
				{
					if ((wp.Sources & TranslationSources.Transfer) > 0)
					{
						matrix[wp.SourceIndex, j] = AlignmentType.Aligned;
						iAligned.Add(wp.SourceIndex);
						jAligned = true;
					}
				}

				if (jAligned)
				{
					for (int i = 0; i < SourceSegment.Count; i++)
					{
						if (matrix[i, j] == AlignmentType.Unknown)
							matrix[i, j] = AlignmentType.NotAligned;
					}
				}
			}

			foreach (int i in iAligned)
			{
				for (int j = 0; j < Prefix.Count; j++)
				{
					if (matrix[i, j] == AlignmentType.Unknown)
						matrix[i, j] = AlignmentType.NotAligned;
				}
			}

			_smtEngine.Train(SourceSegment, Prefix, matrix);
		}

		protected override void DisposeManagedResources()
		{
			_smtSession.Dispose();
			_engine.RemoveSession(this);
		}
	}
}
