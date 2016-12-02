using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.ObjectModel;
using SIL.Progress;

namespace SIL.Machine.Translation
{
	public class HybridTranslationEngine : DisposableBase, IInteractiveTranslationEngine
	{
		internal const double RuleEngineThreshold = 0.05;

		private readonly HashSet<HybridTranslationSession> _sessions;

		public HybridTranslationEngine(IInteractiveSmtEngine smtEngine, ITranslationEngine ruleEngine = null)
		{
			SmtEngine = smtEngine;
			RuleEngine = ruleEngine;
			_sessions = new HashSet<HybridTranslationSession>();
			SourcePreprocessor = s => s;
			TargetPreprocessor = s => s;
		}

		public IInteractiveSmtEngine SmtEngine { get; }
		public ITranslationEngine RuleEngine { get; }

		public Func<string, string> SourcePreprocessor { get; set; }
		public Func<string, string> TargetPreprocessor { get; set; }

		public ITokenizer<string, int> SourceTokenizer { get; set; }
		public ITokenizer<string, int> TargetTokenizer { get; set; }

		public ITextCorpus SourceCorpus { get; set; }
		public ITextCorpus TargetCorpus { get; set; }

		public void Rebuild(IProgress progress = null)
		{
			CheckDisposed();
			CheckSourceTokenizer();
			CheckTargetTokenizer();
			if (SourceCorpus == null)
				throw new InvalidOperationException("A source corpus is not specified.");
			if (TargetCorpus == null)
				throw new InvalidOperationException("A target corpus is not specified");

			lock (_sessions)
			{
				if (_sessions.Count > 0)
					throw new InvalidOperationException("The engine cannot be trained while there are active sessions open.");

				SmtEngine.Train(SourcePreprocessor, SourceTokenizer, SourceCorpus, TargetPreprocessor, TargetTokenizer, TargetCorpus, progress);
			}
		}

		public void Save()
		{
			CheckDisposed();

			SmtEngine.Save();
		}

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

		IInteractiveTranslationSession IInteractiveTranslationEngine.StartSession()
		{
			return StartSession();
		}

		public HybridTranslationSession StartSession()
		{
			CheckDisposed();

			var session = new HybridTranslationSession(this, SmtEngine.StartSession());
			lock (_sessions)
				_sessions.Add(session);
			return session;
		}

		internal static IEnumerable<string> Preprocess(Func<string, string> preprocessor, ITokenizer<string, int> tokenizer, string segment)
		{
			return tokenizer.TokenizeToStrings(preprocessor(segment));
		}

		internal void RemoveSession(HybridTranslationSession session)
		{
			lock (_sessions)
				_sessions.Remove(session);
		}

		protected override void DisposeManagedResources()
		{
			lock (_sessions)
			{
				foreach (HybridTranslationSession session in _sessions.ToArray())
					session.Dispose();
			}

			SmtEngine.Dispose();
			RuleEngine?.Dispose();
		}
	}
}
