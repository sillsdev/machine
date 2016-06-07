using System;
using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;
using SIL.Progress;

namespace SIL.Machine.Translation
{
	public class HybridTranslationEngine : DisposableBase, IInteractiveTranslationEngine
	{
		private const double SecondaryEngineThreshold = 0.05;

		private readonly ITranslationEngine _ruleBasedEngine;
		private readonly IInteractiveSmtEngine _smtEngine;
		private readonly HashSet<HybridTranslationSession> _sessions;

		public HybridTranslationEngine(IInteractiveSmtEngine smtEngine, ITranslationEngine ruleBasedEngine = null)
		{
			_smtEngine = smtEngine;
			_ruleBasedEngine = ruleBasedEngine;
			_sessions = new HashSet<HybridTranslationSession>();
		}

		public IEnumerable<IEnumerable<string>> SourceCorpus { get; set; }
		public IEnumerable<IEnumerable<string>> TargetCorpus { get; set; }

		public void Rebuild(IProgress progress = null)
		{
			CheckDisposed();

			lock (_sessions)
			{
				if (_sessions.Count > 0)
					throw new InvalidOperationException("The engine cannot be trained while there are active sessions open.");

				if (SourceCorpus != null && TargetCorpus != null)
					_smtEngine.Train(SourceCorpus, TargetCorpus, progress);
			}
		}

		public void Save()
		{
			CheckDisposed();

			_smtEngine.Save();
		}

		public TranslationResult Translate(IEnumerable<string> sourceSegment)
		{
			CheckDisposed();

			TranslationResult smtResult = _smtEngine.Translate(sourceSegment);
			TranslationResult transferResult = _ruleBasedEngine.Translate(smtResult.SourceSegment);
			return MergeTranslationResults(smtResult, transferResult);
		}

		internal static TranslationResult MergeTranslationResults(TranslationResult primaryResult, TranslationResult secondaryResult)
		{
			IReadOnlyList<string> sourceSegment = primaryResult.SourceSegment;
			var targetSegment = new List<string>();
			var confidences = new List<double>();
			var alignment = new Dictionary<Tuple<int, int>, AlignedWordPair>();
			for (int j = 0; j < primaryResult.TargetSegment.Count; j++)
			{
				AlignedWordPair[] smtWordPairs = primaryResult.GetTargetWordPairs(j).ToArray();

				if (smtWordPairs.Length == 0)
				{
					targetSegment.Add(primaryResult.TargetSegment[j]);
					confidences.Add(primaryResult.GetTargetWordConfidence(j));
				}
				else
				{
					if (primaryResult.GetTargetWordConfidence(j) >= SecondaryEngineThreshold)
					{
						targetSegment.Add(primaryResult.TargetSegment[j]);
						confidences.Add(primaryResult.GetTargetWordConfidence(j));
						foreach (AlignedWordPair smtWordPair in smtWordPairs)
						{
							TranslationSources sources = smtWordPair.Sources;
							foreach (AlignedWordPair transferWordPair in secondaryResult.GetSourceWordPairs(smtWordPair.SourceIndex))
							{
								if (transferWordPair.Sources != TranslationSources.None
									&& secondaryResult.TargetSegment[transferWordPair.TargetIndex] == primaryResult.TargetSegment[j])
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
							foreach (AlignedWordPair transferWordPair in secondaryResult.GetSourceWordPairs(smtWordPair.SourceIndex))
							{
								if (transferWordPair.Sources != TranslationSources.None)
								{
									targetSegment.Add(secondaryResult.TargetSegment[transferWordPair.TargetIndex]);
									confidences.Add(transferWordPair.Confidence);
									alignment[Tuple.Create(transferWordPair.SourceIndex, targetSegment.Count - 1)] = new AlignedWordPair(transferWordPair.SourceIndex,
										targetSegment.Count - 1, transferWordPair.Confidence, transferWordPair.Sources);
									found = true;
								}
							}
						}

						if (!found)
						{
							targetSegment.Add(primaryResult.TargetSegment[j]);
							confidences.Add(primaryResult.GetTargetWordConfidence(j));
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

			return new TranslationResult(primaryResult.SourceSegment, targetSegment, confidences, alignmentMatrix);
		}

		public IInteractiveTranslationSession StartSession()
		{
			CheckDisposed();

			var session = new HybridTranslationSession(this, _smtEngine.StartSession(), _ruleBasedEngine);
			lock (_sessions)
				_sessions.Add(session);
			return session;
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
			_ruleBasedEngine.Dispose();
		}
	}
}
