using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class HybridTranslationEngine : DisposableBase, IInteractiveTranslationEngine
	{
		internal const double RuleEngineThreshold = 0.05;

		public HybridTranslationEngine(IInteractiveTranslationEngine interactiveEngine,
			ITranslationEngine ruleEngine = null)
		{
			InteractiveEngine = interactiveEngine;
			RuleEngine = ruleEngine;
		}

		public IInteractiveTranslationEngine InteractiveEngine { get; }
		public ITranslationEngine RuleEngine { get; }

		public TranslationResult Translate(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			TranslationResult smtResult = InteractiveEngine.Translate(segment);
			if (RuleEngine == null)
				return smtResult;

			TranslationResult ruleResult = RuleEngine.Translate(segment);
			return smtResult.Merge(RuleEngineThreshold, ruleResult);
		}

		public IEnumerable<TranslationResult> Translate(int n, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			TranslationResult ruleResult = null;
			foreach (TranslationResult smtResult in InteractiveEngine.Translate(n, segment))
			{
				if (RuleEngine == null)
				{
					yield return smtResult;
				}
				else
				{
					if (ruleResult == null)
						ruleResult = RuleEngine.Translate(segment);
					yield return smtResult.Merge(RuleEngineThreshold, ruleResult);
				}
			}
		}

		public WordGraph GetWordGraph(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			WordGraph wordGraph = InteractiveEngine.GetWordGraph(segment);
			if (RuleEngine == null)
				return wordGraph;

			TranslationResult ruleResult = RuleEngine.Translate(segment);
			return wordGraph.Merge(RuleEngineThreshold, ruleResult);
		}

		public void TrainSegment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			InteractiveEngine.TrainSegment(sourceSegment, targetSegment);
		}

		protected override void DisposeManagedResources()
		{
			InteractiveEngine.Dispose();
			RuleEngine?.Dispose();
		}
	}
}
