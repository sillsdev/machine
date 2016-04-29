using System;
using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class TranslationSession : DisposableBase, IInteractiveTranslator
	{
		private const double SecondaryEngineThreshold = 0.05;

		private readonly TranslationEngine _engine;
		private readonly ISmtSession _smtSession;
		private readonly TransferEngine _transferEngine;
		private TranslationResult _transferResult;

		internal TranslationSession(TranslationEngine engine, ISmtSession smtSession, TransferEngine transferEngine)
		{
			_engine = engine;
			_smtSession = smtSession;
			_transferEngine = transferEngine;
		}

		public TranslationResult Translate(IEnumerable<string> sourceSegment)
		{
			TranslationResult smtResult = _smtSession.Translate(sourceSegment);
			TranslationResult transferResult = _transferEngine.Translate(smtResult.SourceSegment);
			return MergeTranslationResults(smtResult, transferResult);
		}

		public IReadOnlyList<string> SourceSegment
		{
			get { return _smtSession.SourceSegment; }
		}

		public IReadOnlyList<string> Prefix
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
			return MergeTranslationResults(smtResult, _transferResult);
		}

		public TranslationResult SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial)
		{
			return MergeTranslationResults(_smtSession.SetPrefix(prefix, isLastWordPartial), _transferResult);
		}

		public TranslationResult AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial)
		{
			return MergeTranslationResults(_smtSession.AddToPrefix(addition, isLastWordPartial), _transferResult);
		}

		public void Reset()
		{
			_transferResult = null;
			_smtSession.Reset();
		}

		public void Approve()
		{
			_smtSession.Train(_smtSession.SourceSegment, _smtSession.Prefix);
		}

		private TranslationResult MergeTranslationResults(TranslationResult primaryResult, TranslationResult secondaryResult)
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

		protected override void DisposeManagedResources()
		{
			_smtSession.Dispose();
			_engine.RemoveSession(this);
		}
	}
}
