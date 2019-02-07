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
		private IReadOnlyList<TranslationResult> _currentResults;

		internal HybridInteractiveTranslationSession(HybridTranslationEngine engine,
			IInteractiveTranslationSession smtSession, TranslationResult ruleResult)
		{
			_engine = engine;
			_smtSession = smtSession;
			_ruleResult = ruleResult;
			UpdateCurrentResults();
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

		public IReadOnlyList<TranslationResult> CurrentResults
		{
			get
			{
				CheckDisposed();
				return _currentResults;
			}
		}

		public IReadOnlyList<TranslationResult> SetPrefix(IReadOnlyList<string> prefix, bool isLastWordComplete)
		{
			CheckDisposed();

			if (!_smtSession.Prefix.SequenceEqual(prefix) || IsLastWordComplete != isLastWordComplete)
			{
				_smtSession.SetPrefix(prefix, isLastWordComplete);
				UpdateCurrentResults();
			}
			return _currentResults;
		}

		public IReadOnlyList<TranslationResult> AppendToPrefix(string addition, bool isLastWordComplete)
		{
			CheckDisposed();

			if (string.IsNullOrEmpty(addition) && IsLastWordComplete)
			{
				throw new ArgumentException(
					"An empty string cannot be added to a prefix where the last word is complete.", nameof(addition));
			}

			if (!string.IsNullOrEmpty(addition) || isLastWordComplete != IsLastWordComplete)
			{
				_smtSession.AppendToPrefix(addition, isLastWordComplete);
				UpdateCurrentResults();
			}
			return _currentResults;
		}

		public IReadOnlyList<TranslationResult> AppendToPrefix(IEnumerable<string> words)
		{
			CheckDisposed();

			int prevPrefixCount = Prefix.Count;
			bool prevIsLastWordComplete = IsLastWordComplete;

			_smtSession.AppendToPrefix(words);

			if (prevPrefixCount != Prefix.Count || prevIsLastWordComplete != IsLastWordComplete)
				UpdateCurrentResults();

			return _currentResults;
		}

		public void Approve()
		{
			CheckDisposed();

			_engine.SmtEngine.TrainSegment(SourceSegment, Prefix);
		}

		private void UpdateCurrentResults()
		{
			if (_ruleResult == null)
			{
				_currentResults = _smtSession.CurrentResults;
			}
			else
			{
				int prefixCount = Prefix.Count;
				if (!IsLastWordComplete)
					prefixCount--;

				_currentResults = _smtSession.CurrentResults
					.Select(r => r.Merge(prefixCount, HybridTranslationEngine.RuleEngineThreshold, _ruleResult))
					.ToArray();
			}
		}

		protected override void DisposeManagedResources()
		{
			_smtSession.Dispose();
			_engine.RemoveSession(this);
		}
	}
}
