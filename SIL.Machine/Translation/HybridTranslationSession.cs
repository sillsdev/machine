using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	internal class HybridTranslationSession : DisposableBase, IInteractiveTranslationSession
	{
		private readonly HybridTranslationEngine _engine;
		private readonly IInteractiveSmtSession _smtSession;
		private readonly ITranslationEngine _transferEngine;
		private TranslationResult _lastResult;
		private TranslationResult _transferResult;

		public HybridTranslationSession(HybridTranslationEngine engine, IInteractiveSmtSession smtSession, ITranslationEngine transferEngine)
		{
			_engine = engine;
			_smtSession = smtSession;
			_transferEngine = transferEngine;
		}

		public ReadOnlyList<string> SourceSegment
		{
			get { return _smtSession.SourceSegment; }
		}

		public ReadOnlyList<string> Prefix
		{
			get { return _smtSession.Prefix; }
		}

		public bool IsLastWordPartial
		{
			get { return _smtSession.IsLastWordPartial; }
		}

		public TranslationResult TranslateInteractively(IEnumerable<string> sourceSegment)
		{
			TranslationResult smtResult = _smtSession.TranslateInteractively(sourceSegment);
			_transferResult = _transferEngine.Translate(smtResult.SourceSegment);
			_lastResult = HybridTranslationEngine.MergeTranslationResults(smtResult, _transferResult);
			return _lastResult;
		}

		public TranslationResult SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial)
		{
			_lastResult = HybridTranslationEngine.MergeTranslationResults(_smtSession.SetPrefix(prefix, isLastWordPartial), _transferResult);
			return _lastResult;
		}

		public TranslationResult AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			_lastResult = HybridTranslationEngine.MergeTranslationResults(_smtSession.AddToPrefix(addition, isLastWordPartial), _transferResult);
			return _lastResult;
		}

		public void Reset()
		{
			_lastResult = null;
			_transferResult = null;
			_smtSession.Reset();
		}

		public void Approve()
		{
			_smtSession.Approve();
			for (int j = 0; j < _smtSession.Prefix.Count; j++)
			{
				foreach (AlignedWordPair wp in _lastResult.GetTargetWordPairs(j))
				{
					if ((wp.Sources & TranslationSources.Transfer) == TranslationSources.Transfer)
						_smtSession.Train(new[] {_lastResult.SourceSegment[wp.SourceIndex], "."}, new[] {_lastResult.TargetSegment[j], "."});
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
