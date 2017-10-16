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

		IInteractiveTranslationSession IInteractiveTranslationEngine.TranslateInteractively(IReadOnlyList<string> segment)
		{
			return TranslateInteractively(segment);
		}

		public HybridInteractiveTranslationSession TranslateInteractively(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			IInteractiveTranslationSession smtSession = SmtEngine.TranslateInteractively(segment);
			TranslationResult ruleResult = RuleEngine?.Translate(segment);
			var session = new HybridInteractiveTranslationSession(this, smtSession, ruleResult);
			_sessions.Add(session);
			return session;
		}

		public WordAlignmentMatrix TrainSegment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			TranslationResult ruleResult = RuleEngine?.Translate(sourceSegment);

			WordAlignmentMatrix matrix = GetHintMatrix(sourceSegment, targetSegment, ruleResult);
			SmtEngine.TrainSegment(sourceSegment, targetSegment, matrix);
			return matrix;
		}

		internal WordAlignmentMatrix GetHintMatrix(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment, TranslationResult ruleResult)
		{
			TranslationResult smtResult = SmtEngine.GetBestPhraseAlignment(sourceSegment, targetSegment);
			TranslationResult hybridResult = ruleResult == null ? smtResult : smtResult.Merge(targetSegment.Count, RuleEngineThreshold, ruleResult);

			var matrix = new WordAlignmentMatrix(sourceSegment.Count, targetSegment.Count, AlignmentType.Unknown);
			var iAligned = new HashSet<int>();
			for (int j = 0; j < targetSegment.Count; j++)
			{
				bool jAligned = false;
				if ((hybridResult.WordSources[j] & TranslationSources.Transfer) != 0)
				{
					foreach (int i in hybridResult.Alignment.GetColumnAlignedIndices(j))
					{
						matrix[i, j] = AlignmentType.Aligned;
						iAligned.Add(i);
						jAligned = true;
					}
				}

				if (jAligned)
				{
					for (int i = 0; i < sourceSegment.Count; i++)
					{
						if (matrix[i, j] == AlignmentType.Unknown)
							matrix[i, j] = AlignmentType.NotAligned;
					}
				}
			}

			foreach (int i in iAligned)
			{
				for (int j = 0; j < targetSegment.Count; j++)
				{
					if (matrix[i, j] == AlignmentType.Unknown)
						matrix[i, j] = AlignmentType.NotAligned;
				}
			}

			return matrix;
		}

		internal void RemoveSession(HybridInteractiveTranslationSession session)
		{
			_sessions.Remove(session);
		}

		protected override void DisposeManagedResources()
		{
			foreach (HybridInteractiveTranslationSession session in _sessions.ToArray())
				session.Dispose();
		}
	}
}
