using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	internal class HybridTranslationSession : DisposableBase, IInteractiveTranslationSession
	{
		private readonly HybridTranslationEngine _engine;
		private readonly IInteractiveSmtSession _smtSession;
		private readonly ITranslationEngine _transferEngine;
		private TranslationResult _currentResult;
		private TranslationResult _transferResult;

		public HybridTranslationSession(HybridTranslationEngine engine, IInteractiveSmtSession smtSession, ITranslationEngine transferEngine)
		{
			_engine = engine;
			_smtSession = smtSession;
			_transferEngine = transferEngine;
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

		public bool IsLastWordPartial
		{
			get
			{
				CheckDisposed();
				return _smtSession.IsLastWordPartial;
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

			TranslationResult smtResult = _smtSession.TranslateInteractively(sourceSegment);
			_transferResult = _transferEngine.Translate(smtResult.SourceSegment);
			_currentResult = HybridTranslationEngine.MergeTranslationResults(smtResult, _transferResult);
			return _currentResult;
		}

		public TranslationResult SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial)
		{
			CheckDisposed();

			_currentResult = HybridTranslationEngine.MergeTranslationResults(_smtSession.SetPrefix(prefix, isLastWordPartial), _transferResult);
			return _currentResult;
		}

		public TranslationResult AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			CheckDisposed();

			_currentResult = HybridTranslationEngine.MergeTranslationResults(_smtSession.AddToPrefix(addition, isLastWordPartial), _transferResult);
			return _currentResult;
		}

		public void Reset()
		{
			CheckDisposed();

			_currentResult = null;
			_transferResult = null;
			_smtSession.Reset();
		}

		public void Approve()
		{
			CheckDisposed();

			_smtSession.Approve();
			for (int j = 0; j < _smtSession.Prefix.Count; j++)
			{
				foreach (AlignedWordPair wp in _currentResult.GetTargetWordPairs(j))
				{
					if ((wp.Sources & TranslationSources.Transfer) == TranslationSources.Transfer)
						_smtSession.Train(new[] {_currentResult.SourceSegment[wp.SourceIndex], "."}, new[] {_currentResult.TargetSegment[j], "."});
				}
			}
		}

		protected override void DisposeManagedResources()
		{
			_smtSession.Dispose();
			_engine.RemoveSession(this);
		}
	}
}
