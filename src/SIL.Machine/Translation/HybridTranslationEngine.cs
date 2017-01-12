using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Tokenization;
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
			SourcePreprocessor = s => s;
			TargetPreprocessor = s => s;
		}

		public IInteractiveSmtEngine SmtEngine { get; }
		public ITranslationEngine RuleEngine { get; }

		public Func<string, string> SourcePreprocessor { get; set; }
		public Func<string, string> TargetPreprocessor { get; set; }

		public ITokenizer<string, int> SourceTokenizer { get; set; }
		public ITokenizer<string, int> TargetTokenizer { get; set; }

		public TranslationResult Translate(IEnumerable<string> segment)
		{
			CheckDisposed();

			TranslationResult smtResult = SmtEngine.Translate(segment);
			if (RuleEngine == null)
				return smtResult;

			TranslationResult ruleResult = RuleEngine.Translate(smtResult.SourceSegment);
			return smtResult.Merge(0, RuleEngineThreshold, ruleResult);
		}

		public IEnumerable<TranslationResult> Translate(int n, IEnumerable<string> segment)
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
						ruleResult = RuleEngine.Translate(smtResult.SourceSegment);
					yield return smtResult.Merge(0, RuleEngineThreshold, ruleResult);
				}
			}
		}

		public TranslationResult Translate(string sourceSegment)
		{
			CheckDisposed();
			CheckSourceTokenizer();

			return Translate(Preprocess(SourcePreprocessor, SourceTokenizer, sourceSegment));
		}

		IInteractiveTranslationSession IInteractiveTranslationEngine.TranslateInteractively(IEnumerable<string> segment)
		{
			return TranslateInteractively(segment);
		}

		public HybridInteractiveTranslationSession TranslateInteractively(IEnumerable<string> segment)
		{
			CheckDisposed();

			IInteractiveTranslationSession smtSession = SmtEngine.TranslateInteractively(segment);
			TranslationResult ruleResult = RuleEngine?.Translate(smtSession.SourceSegment);
			var session = new HybridInteractiveTranslationSession(this, smtSession, ruleResult);
			lock (_sessions)
				_sessions.Add(session);
			return session;
		}

		public HybridInteractiveTranslationSession TranslateInteractively(string segment)
		{
			CheckDisposed();
			CheckSourceTokenizer();

			return TranslateInteractively(Preprocess(SourcePreprocessor, SourceTokenizer, segment));
		}

		public void TrainSegment(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment)
		{
			CheckDisposed();

			string[] sourceSegmentArray = sourceSegment.ToArray();
			string[] targetSegmentArray = targetSegment.ToArray();

			TranslationResult ruleResult = RuleEngine?.Translate(sourceSegmentArray);
			TrainSegment(sourceSegmentArray, targetSegmentArray, ruleResult);
		}

		public void TrainSegment(string sourceSegment, string targetSegment)
		{
			CheckDisposed();
			CheckSourceTokenizer();
			CheckTargetTokenizer();

			TrainSegment(Preprocess(SourcePreprocessor, SourceTokenizer, sourceSegment), Preprocess(TargetPreprocessor, TargetTokenizer, targetSegment));
		}

		internal void TrainSegment(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment, TranslationResult ruleResult)
		{
			TranslationResult smtResult = SmtEngine.GetBestPhraseAlignment(sourceSegment, targetSegment);
			TranslationResult hybridResult = ruleResult == null ? smtResult : smtResult.Merge(targetSegment.Count, RuleEngineThreshold, ruleResult);

			var matrix = new WordAlignmentMatrix(sourceSegment.Count, targetSegment.Count, AlignmentType.Unknown);
			var iAligned = new HashSet<int>();
			for (int j = 0; j < targetSegment.Count; j++)
			{
				bool jAligned = false;
				if ((hybridResult.TargetWordSources[j] & TranslationSources.Transfer) != 0)
				{
					foreach (int i in hybridResult.Alignment.GetColumnWordAlignedIndices(j))
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

			SmtEngine.TrainSegment(sourceSegment, targetSegment, matrix);
		}

		internal void CheckSourceTokenizer()
		{
			if (SourceTokenizer == null)
				throw new InvalidOperationException("A source tokenizer is not specified.");
		}

		internal void CheckTargetTokenizer()
		{
			if (TargetTokenizer == null)
				throw new InvalidOperationException("A target tokenizer is not specified.");
		}

		internal static IEnumerable<string> Preprocess(Func<string, string> preprocessor, ITokenizer<string, int> tokenizer, string segment)
		{
			return tokenizer.TokenizeToStrings(preprocessor(segment));
		}

		internal void RemoveSession(HybridInteractiveTranslationSession session)
		{
			lock (_sessions)
				_sessions.Remove(session);
		}

		protected override void DisposeManagedResources()
		{
			lock (_sessions)
			{
				foreach (HybridInteractiveTranslationSession session in _sessions.ToArray())
					session.Dispose();
			}

			SmtEngine.Dispose();
			RuleEngine?.Dispose();
		}
	}
}
