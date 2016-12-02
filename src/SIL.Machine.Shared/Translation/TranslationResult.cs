using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation
{
	public class TranslationResult
	{
		private readonly AlignedWordPair[,] _alignment;

		public TranslationResult(IEnumerable<string> sourceSegment, IEnumerable<string> targetSegment, IEnumerable<double> confidences, AlignedWordPair[,] alignment)
		{
			SourceSegment = sourceSegment.ToArray();
			TargetSegment = targetSegment.ToArray();
			TargetWordConfidences = confidences.ToArray();
			_alignment = alignment;
		}

		public IReadOnlyList<string> SourceSegment { get; }

		public IReadOnlyList<string> TargetSegment { get; }

		public IReadOnlyList<double> TargetWordConfidences { get; }

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
				AlignedWordPair[] wordPairs = GetTargetWordPairs(j).ToArray();

				if (wordPairs.Length == 0)
				{
					targetSegment.Add(TargetSegment[j]);
					confidences.Add(TargetWordConfidences[j]);
				}
				else
				{
					if (j < prefixCount || TargetWordConfidences[j] >= threshold)
					{
						targetSegment.Add(TargetSegment[j]);
						confidences.Add(TargetWordConfidences[j]);
						foreach (AlignedWordPair wordPair in wordPairs)
						{
							TranslationSources sources = wordPair.Sources;
							foreach (AlignedWordPair transferWordPair in otherResult.GetSourceWordPairs(wordPair.SourceIndex))
							{
								if (transferWordPair.Sources != TranslationSources.None
									&& otherResult.TargetSegment[transferWordPair.TargetIndex] == TargetSegment[j])
								{
									sources |= transferWordPair.Sources;
								}
							}

							alignment[Tuple.Create(wordPair.SourceIndex, targetSegment.Count - 1)] = new AlignedWordPair(wordPair.SourceIndex,
								targetSegment.Count - 1, sources);
						}
					}
					else
					{
						bool found = false;
						foreach (AlignedWordPair wordPair in wordPairs)
						{
							foreach (AlignedWordPair otherWordPair in otherResult.GetSourceWordPairs(wordPair.SourceIndex))
							{
								if (otherWordPair.Sources != TranslationSources.None)
								{
									targetSegment.Add(otherResult.TargetSegment[otherWordPair.TargetIndex]);
									confidences.Add(otherResult.TargetWordConfidences[otherWordPair.TargetIndex]);
									alignment[Tuple.Create(otherWordPair.SourceIndex, targetSegment.Count - 1)] = new AlignedWordPair(otherWordPair.SourceIndex,
										targetSegment.Count - 1, otherWordPair.Sources);
									found = true;
								}
							}
						}

						if (!found)
						{
							targetSegment.Add(TargetSegment[j]);
							confidences.Add(TargetWordConfidences[j]);
							foreach (AlignedWordPair wordPair in wordPairs)
							{
								alignment[Tuple.Create(wordPair.SourceIndex, targetSegment.Count - 1)] = new AlignedWordPair(wordPair.SourceIndex,
									targetSegment.Count - 1, wordPair.Sources);
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
