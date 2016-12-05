using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class HybridInteractiveTranslationSession : DisposableBase, IInteractiveTranslationSession
	{
		private readonly HybridTranslationEngine _engine;
		private readonly IInteractiveTranslationSession _smtSession;
		private readonly TranslationResult _ruleResult;
		private TranslationResult _currentResult;

		internal HybridInteractiveTranslationSession(HybridTranslationEngine engine, IInteractiveTranslationSession smtSession, TranslationResult ruleResult)
		{
			_engine = engine;
			_smtSession = smtSession;
			_ruleResult = ruleResult;
			_currentResult = _ruleResult == null ? _smtSession.CurrentTranslationResult
				: _smtSession.CurrentTranslationResult.Merge(0, HybridTranslationEngine.RuleEngineThreshold, _ruleResult);
		}

		public IReadOnlyList<string> SourceSegment
		{
			get
			{
				CheckDisposed();
				return _smtSession.SourceSegment;
			}
		}

		public IReadOnlyList<string> Prefix
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

		public TranslationResult CurrentTranslationResult
		{
			get
			{
				CheckDisposed();
				return _currentResult;
			}
		}

		public TranslationResult SetPrefix(IEnumerable<string> prefix, bool isLastWordComplete)
		{
			CheckDisposed();

			TranslationResult smtResult = _smtSession.SetPrefix(prefix, isLastWordComplete);
			int prefixCount = _smtSession.Prefix.Count;
			if (!_smtSession.IsLastWordComplete)
				prefixCount--;
			_currentResult = _ruleResult == null ? smtResult : smtResult.Merge(prefixCount, HybridTranslationEngine.RuleEngineThreshold, _ruleResult);
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
			_currentResult = _ruleResult == null ? smtResult : smtResult.Merge(prefixCount, HybridTranslationEngine.RuleEngineThreshold, _ruleResult);
			return _currentResult;
		}

		public TranslationResult AddToPrefix(string addition, bool isLastWordComplete)
		{
			CheckDisposed();
			_engine.CheckTargetTokenizer();

			return AddToPrefix(HybridTranslationEngine.Preprocess(_engine.TargetPreprocessor, _engine.TargetTokenizer, addition), isLastWordComplete);
		}

		public void Approve()
		{
			CheckDisposed();

			_engine.TrainSegment(SourceSegment, Prefix, _ruleResult);
		}

		protected override void DisposeManagedResources()
		{
			_smtSession.Dispose();
			_engine.RemoveSession(this);
		}
	}
}
