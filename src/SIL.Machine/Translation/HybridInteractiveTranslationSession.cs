using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;
using System;

namespace SIL.Machine.Translation
{
	public class HybridInteractiveTranslationSession : DisposableBase, IInteractiveTranslationSession
	{
		private readonly HybridTranslationEngine _engine;
		private readonly IInteractiveTranslationSession _smtSession;
		private readonly TranslationResult _ruleResult;
		private TranslationResult _currentResult;

		internal HybridInteractiveTranslationSession(HybridTranslationEngine engine,
			IInteractiveTranslationSession smtSession, TranslationResult ruleResult)
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

			if (!_smtSession.Prefix.SequenceEqual(prefix) || IsLastWordComplete != isLastWordComplete)
			{
				TranslationResult smtResult = _smtSession.SetPrefix(prefix, isLastWordComplete);
				UpdateCurrentResult(smtResult);
			}
			return _currentResult;
		}

		public TranslationResult AppendToPrefix(string addition, bool isLastWordComplete)
		{
			CheckDisposed();

			if (string.IsNullOrEmpty(addition) && IsLastWordComplete)
			{
				throw new ArgumentException(
					"An empty string cannot be added to a prefix where the last word is complete.", nameof(addition));
			}

			if (!string.IsNullOrEmpty(addition) || isLastWordComplete != IsLastWordComplete)
			{
				TranslationResult smtResult = _smtSession.AppendToPrefix(addition, isLastWordComplete);
				UpdateCurrentResult(smtResult);
			}
			return _currentResult;
		}

		public TranslationResult AppendToPrefix(IEnumerable<string> words)
		{
			CheckDisposed();

			int prevPrefixCount = Prefix.Count;
			bool prevIsLastWordComplete = IsLastWordComplete;

			TranslationResult smtResult = _smtSession.AppendToPrefix(words);

			if (prevPrefixCount != Prefix.Count || prevIsLastWordComplete != IsLastWordComplete)
				UpdateCurrentResult(smtResult);

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

		private void UpdateCurrentResult(TranslationResult smtResult)
		{
			int prefixCount = Prefix.Count;
			if (!IsLastWordComplete)
				prefixCount--;
			_currentResult = _ruleResult == null ? smtResult
				: smtResult.Merge(prefixCount, HybridTranslationEngine.RuleEngineThreshold, _ruleResult);
		}

		protected override void DisposeManagedResources()
		{
			_smtSession.Dispose();
			_engine.RemoveSession(this);
		}
	}
}
