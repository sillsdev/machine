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

		private readonly ITranslationEngine _ruleEngine;
		private readonly IInteractiveSmtEngine _smtEngine;
		private readonly HashSet<HybridTranslationSession> _sessions;

		public HybridTranslationEngine(IInteractiveSmtEngine smtEngine, ITranslationEngine ruleEngine = null)
		{
			_smtEngine = smtEngine;
			_ruleEngine = ruleEngine;
			_sessions = new HashSet<HybridTranslationSession>();
			SourcePreprocessor = s => s;
			TargetPreprocessor = s => s;
		}

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

				_smtEngine.Train(SourcePreprocessor, SourceTokenizer, SourceCorpus, TargetPreprocessor, TargetTokenizer, TargetCorpus, progress);
			}
		}

		public void Save()
		{
			CheckDisposed();

			_smtEngine.Save();
		}

		public TranslationResult Translate(IEnumerable<string> segment)
		{
			CheckDisposed();

			TranslationResult ruleResult = _ruleEngine.Translate(segment);
			TranslationResult smtResult = _smtEngine.Translate(ruleResult.SourceSegment);
			return smtResult.Merge(0, RuleEngineThreshold, ruleResult);
		}

		public IEnumerable<TranslationResult> Translate(int n, IEnumerable<string> segment)
		{
			CheckDisposed();

			TranslationResult ruleResult = _ruleEngine.Translate(segment);
			return _smtEngine.Translate(n, ruleResult.SourceSegment)
				.Select(smtResult => smtResult.Merge(0, RuleEngineThreshold, ruleResult));
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

			var session = new HybridTranslationSession(this, _smtEngine, _smtEngine.StartSession(), _ruleEngine);
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

			_smtEngine.Dispose();
			_ruleEngine.Dispose();
		}
	}
}
