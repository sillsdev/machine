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
		private const double SecondaryEngineThreshold = 0.05;

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
			return MergeTranslationResults(0, smtResult, ruleResult);
		}

		public IEnumerable<TranslationResult> Translate(int n, IEnumerable<string> segment)
		{
			CheckDisposed();

			TranslationResult ruleResult = _ruleEngine.Translate(segment);
			return _smtEngine.Translate(n, ruleResult.SourceSegment).Select(smtResult => MergeTranslationResults(0, smtResult, ruleResult));
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

		internal static TranslationResult MergeTranslationResults(int prefixCount, TranslationResult smtResult, TranslationResult ruleResult)
		{
			IReadOnlyList<string> sourceSegment = smtResult.SourceSegment;
			var targetSegment = new List<string>();
			var confidences = new List<double>();
			var alignment = new Dictionary<Tuple<int, int>, AlignedWordPair>();
			for (int j = 0; j < smtResult.TargetSegment.Count; j++)
			{
				AlignedWordPair[] smtWordPairs = smtResult.GetTargetWordPairs(j).ToArray();

				if (smtWordPairs.Length == 0)
				{
					targetSegment.Add(smtResult.TargetSegment[j]);
					confidences.Add(smtResult.GetTargetWordConfidence(j));
				}
				else
				{
					if (j < prefixCount || smtResult.GetTargetWordConfidence(j) >= SecondaryEngineThreshold)
					{
						targetSegment.Add(smtResult.TargetSegment[j]);
						confidences.Add(smtResult.GetTargetWordConfidence(j));
						foreach (AlignedWordPair smtWordPair in smtWordPairs)
						{
							TranslationSources sources = smtWordPair.Sources;
							foreach (AlignedWordPair transferWordPair in ruleResult.GetSourceWordPairs(smtWordPair.SourceIndex))
							{
								if (transferWordPair.Sources != TranslationSources.None
									&& ruleResult.TargetSegment[transferWordPair.TargetIndex] == smtResult.TargetSegment[j])
								{
									sources |= transferWordPair.Sources;
								}
							}

							alignment[Tuple.Create(smtWordPair.SourceIndex, targetSegment.Count - 1)] = new AlignedWordPair(smtWordPair.SourceIndex,
								targetSegment.Count - 1, smtWordPair.Confidence, sources);
						}
					}
					else
					{
						bool found = false;
						foreach (AlignedWordPair smtWordPair in smtWordPairs)
						{
							foreach (AlignedWordPair transferWordPair in ruleResult.GetSourceWordPairs(smtWordPair.SourceIndex))
							{
								if (transferWordPair.Sources != TranslationSources.None)
								{
									targetSegment.Add(ruleResult.TargetSegment[transferWordPair.TargetIndex]);
									confidences.Add(transferWordPair.Confidence);
									alignment[Tuple.Create(transferWordPair.SourceIndex, targetSegment.Count - 1)] = new AlignedWordPair(transferWordPair.SourceIndex,
										targetSegment.Count - 1, transferWordPair.Confidence, transferWordPair.Sources);
									found = true;
								}
							}
						}

						if (!found)
						{
							targetSegment.Add(smtResult.TargetSegment[j]);
							confidences.Add(smtResult.GetTargetWordConfidence(j));
							foreach (AlignedWordPair smtWordPair in smtWordPairs)
							{
								alignment[Tuple.Create(smtWordPair.SourceIndex, targetSegment.Count - 1)] = new AlignedWordPair(smtWordPair.SourceIndex,
									targetSegment.Count - 1, smtWordPair.Confidence, smtWordPair.Sources);
							}
						}
					}
				}
			}

			AlignedWordPair[,] alignmentMatrix = new AlignedWordPair[sourceSegment.Count, targetSegment.Count];
			foreach (KeyValuePair<Tuple<int, int>, AlignedWordPair> kvp in alignment)
				alignmentMatrix[kvp.Key.Item1, kvp.Key.Item2] = kvp.Value;

			return new TranslationResult(smtResult.SourceSegment, targetSegment, confidences, alignmentMatrix);
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
