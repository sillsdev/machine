using System.Collections.Generic;
using System.Linq;
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
			_currentResult = _ruleResult == null ? _smtSession.CurrentResult
				: _smtSession.CurrentResult.Merge(0, HybridTranslationEngine.RuleEngineThreshold, _ruleResult);
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

		public TranslationResult CurrentResult
		{
			get
			{
				CheckDisposed();
				return _currentResult;
			}
		}

		public TranslationResult SetPrefix(IReadOnlyList<string> prefix, bool isLastWordComplete)
		{
			CheckDisposed();

			if (!_smtSession.Prefix.SequenceEqual(prefix) || _smtSession.IsLastWordComplete != isLastWordComplete)
			{
				TranslationResult smtResult = _smtSession.SetPrefix(prefix, isLastWordComplete);
				int prefixCount = prefix.Count;
				if (!_smtSession.IsLastWordComplete)
					prefixCount--;
				_currentResult = _ruleResult == null ? smtResult : smtResult.Merge(prefixCount, HybridTranslationEngine.RuleEngineThreshold, _ruleResult);
			}
			return _currentResult;
		}

		void IInteractiveTranslationSession.Approve()
		{
			Approve();
		}

		public WordAlignmentMatrix Approve()
		{
			CheckDisposed();

			WordAlignmentMatrix matrix = _engine.GetHintMatrix(SourceSegment, Prefix, _ruleResult);
			_engine.SmtEngine.TrainSegment(SourceSegment, Prefix, matrix);
			return matrix;
		}

		protected override void DisposeManagedResources()
		{
			_smtSession.Dispose();
			_engine.RemoveSession(this);
		}
	}
}
