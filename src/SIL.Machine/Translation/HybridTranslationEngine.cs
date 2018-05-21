using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class HybridTranslationEngine : DisposableBase, IInteractiveTranslationEngine
	{
		internal const double RuleEngineThreshold = 0.05;

		private readonly HashSet<HybridInteractiveTranslationSession> _sessions;

		public HybridTranslationEngine(IInteractiveSmtEngine smtEngine, ITranslationEngine ruleEngine = null)
		{
			SmtEngine = smtEngine;
			RuleEngine = ruleEngine;
			_sessions = new HashSet<HybridInteractiveTranslationSession>();
		}

		public IInteractiveSmtEngine SmtEngine { get; }
		public ITranslationEngine RuleEngine { get; }

		public TranslationResult Translate(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			TranslationResult smtResult = SmtEngine.Translate(segment);
			if (RuleEngine == null)
				return smtResult;

			TranslationResult ruleResult = RuleEngine.Translate(segment);
			return smtResult.Merge(0, RuleEngineThreshold, ruleResult);
		}

		public IEnumerable<TranslationResult> Translate(int n, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			TranslationResult ruleResult = null;
			foreach (TranslationResult smtResult in SmtEngine.Translate(n, segment))
			{
				if (RuleEngine == null)
				{
					yield return smtResult;
				}
				else
				{
					if (ruleResult == null)
						ruleResult = RuleEngine.Translate(segment);
					yield return smtResult.Merge(0, RuleEngineThreshold, ruleResult);
				}
			}
		}

		IInteractiveTranslationSession IInteractiveTranslationEngine.TranslateInteractively(int n,
			IReadOnlyList<string> segment)
		{
			return TranslateInteractively(n, segment);
		}

		public HybridInteractiveTranslationSession TranslateInteractively(int n, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			IInteractiveTranslationSession smtSession = SmtEngine.TranslateInteractively(n, segment);
			TranslationResult ruleResult = RuleEngine?.Translate(segment);
			var session = new HybridInteractiveTranslationSession(this, smtSession, ruleResult);
			_sessions.Add(session);
			return session;
		}

		public HybridInteractiveTranslationResult TranslateInteractively(IReadOnlyList<string> segment)
		{
			WordGraph smtWordGraph = SmtEngine.GetWordGraph(segment);
			TranslationResult ruleResult = RuleEngine?.Translate(segment);
			return new HybridInteractiveTranslationResult(smtWordGraph, ruleResult);
		}

		public void TrainSegment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			SmtEngine.TrainSegment(sourceSegment, targetSegment);
		}

		internal void RemoveSession(HybridInteractiveTranslationSession session)
		{
			_sessions.Remove(session);
		}

		protected override void DisposeManagedResources()
		{
			foreach (HybridInteractiveTranslationSession session in _sessions.ToArray())
				session.Dispose();
			SmtEngine.Dispose();
			RuleEngine?.Dispose();
		}
	}
}
