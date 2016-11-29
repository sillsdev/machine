using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace SIL.Machine.Translation
{
	public class TranslationResult
	{
		private readonly double[] _confidences; 
		private readonly AlignedWordPair[,] _alignment;

		public TranslationResult(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment, IEnumerable<double> confidences, AlignedWordPair[,] alignment)
		{
			SourceSegment = new ReadOnlyCollection<string>(sourceSegment.ToArray());
			TargetSegment = new ReadOnlyCollection<string>(targetSegment.ToArray());
			_confidences = confidences.ToArray();
			_alignment = alignment;
		}

		public IReadOnlyList<string> SourceSegment { get; }

		public IReadOnlyList<string> TargetSegment { get; }

		public double GetTargetWordConfidence(int targetIndex)
		{
			return _confidences[targetIndex];
		}

		public IEnumerable<AlignedWordPair> GetSourceWordPairs(int sourceIndex)
		{
			return Enumerable.Range(0, TargetSegment.Count).Where(j => _alignment[sourceIndex, j] != null).Select(j => _alignment[sourceIndex, j]);
		}

		public IEnumerable<AlignedWordPair> GetTargetWordPairs(int targetIndex)
		{
			return Enumerable.Range(0, SourceSegment.Count).Where(i => _alignment[i, targetIndex] != null).Select(i => _alignment[i, targetIndex]);
		}

		public bool TryGetWordPair(int sourceIndex, int targetIndex, out AlignedWordPair wordPair)
		{
			if (_alignment[sourceIndex, targetIndex] != null)
			{
				wordPair = _alignment[sourceIndex, targetIndex];
				return true;
			}

			wordPair = null;
			return false;
		}

		public TranslationResult Merge(int prefixCount, double threshold, TranslationResult otherResult)
		{
			var targetSegment = new List<string>();
			var confidences = new List<double>();
			var alignment = new Dictionary<Tuple<int, int>, AlignedWordPair>();
			for (int j = 0; j < TargetSegment.Count; j++)
			{
				AlignedWordPair[] smtWordPairs = GetTargetWordPairs(j).ToArray();

				if (smtWordPairs.Length == 0)
				{
					targetSegment.Add(TargetSegment[j]);
					confidences.Add(GetTargetWordConfidence(j));
				}
				else
				{
					if (j < prefixCount || GetTargetWordConfidence(j) >= threshold)
					{
						targetSegment.Add(TargetSegment[j]);
						confidences.Add(GetTargetWordConfidence(j));
						foreach (AlignedWordPair smtWordPair in smtWordPairs)
						{
							TranslationSources sources = smtWordPair.Sources;
							foreach (AlignedWordPair transferWordPair in otherResult.GetSourceWordPairs(smtWordPair.SourceIndex))
							{
								if (transferWordPair.Sources != TranslationSources.None
									&& otherResult.TargetSegment[transferWordPair.TargetIndex] == TargetSegment[j])
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
							foreach (AlignedWordPair transferWordPair in otherResult.GetSourceWordPairs(smtWordPair.SourceIndex))
							{
								if (transferWordPair.Sources != TranslationSources.None)
								{
									targetSegment.Add(otherResult.TargetSegment[transferWordPair.TargetIndex]);
									confidences.Add(transferWordPair.Confidence);
									alignment[Tuple.Create(transferWordPair.SourceIndex, targetSegment.Count - 1)] = new AlignedWordPair(transferWordPair.SourceIndex,
										targetSegment.Count - 1, transferWordPair.Confidence, transferWordPair.Sources);
									found = true;
								}
							}
						}

						if (!found)
						{
							targetSegment.Add(TargetSegment[j]);
							confidences.Add(GetTargetWordConfidence(j));
							foreach (AlignedWordPair smtWordPair in smtWordPairs)
							{
								alignment[Tuple.Create(smtWordPair.SourceIndex, targetSegment.Count - 1)] = new AlignedWordPair(smtWordPair.SourceIndex,
									targetSegment.Count - 1, smtWordPair.Confidence, smtWordPair.Sources);
							}
						}
					}
				}
			}

			AlignedWordPair[,] alignmentMatrix = new AlignedWordPair[SourceSegment.Count, targetSegment.Count];
			foreach (KeyValuePair<Tuple<int, int>, AlignedWordPair> kvp in alignment)
				alignmentMatrix[kvp.Key.Item1, kvp.Key.Item2] = kvp.Value;

			return new TranslationResult(SourceSegment, targetSegment, confidences, alignmentMatrix);
		}
	}
}
