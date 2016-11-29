using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.Machine.Translation
{
	public class ErrorCorrectingModel
	{
		private readonly SegmentEditDistance _segmentEditDistance;

		public ErrorCorrectingModel()
		{
			_segmentEditDistance = new SegmentEditDistance();
			SetErrorModelParameters(128, 0.8, 1, 1, 1);
		}

		public void SetErrorModelParameters(int vocSize, double hitProb, double insFactor, double substFactor, double delFactor)
		{
			double e;
			if (vocSize == 0)
				e = (1 - hitProb) / (insFactor + substFactor + delFactor);
			else
				e = (1 - hitProb) / ((insFactor * vocSize) + (substFactor * (vocSize - 1)) + delFactor);

			double insProb = e * insFactor;
			double substProb = e * substFactor;
			double delProb = e * delFactor;

			_segmentEditDistance.HitCost = -Math.Log(hitProb);
			_segmentEditDistance.InsertionCost = -Math.Log(insProb);
			_segmentEditDistance.SubstitutionCost = -Math.Log(substProb);
			_segmentEditDistance.DeletionCost = -Math.Log(delProb);
		}

		public void SetupInitialEsi(EcmScoreInfo initialEsi)
		{
			double score = _segmentEditDistance.Compute(new string[0], new string[0]);
			initialEsi.Scores.Clear();
			initialEsi.Scores.Add(score);
			initialEsi.Operations.Clear();
		}

		public void SetupEsi(EcmScoreInfo esi, EcmScoreInfo prevEsi, string word)
		{
			double score = _segmentEditDistance.Compute(new[] {word}, new string[0]);
			esi.Scores.Clear();
			esi.Scores.Add(prevEsi.Scores[0] + score);
			esi.Operations.Clear();
			esi.Operations.Add(EditOperation.None);
		}

		public void ExtendInitialEsi(EcmScoreInfo initialEsi, EcmScoreInfo prevInitialEsi, IReadOnlyList<string> prefixDiff)
		{
			_segmentEditDistance.IncrComputePrefixFirstRow(initialEsi.Scores, prevInitialEsi.Scores, prefixDiff);
		}

		public void ExtendEsi(EcmScoreInfo esi, EcmScoreInfo prevEsi, string word, IReadOnlyList<string> prefixDiff, bool isLastWordComplete)
		{
			IEnumerable<EditOperation> ops = _segmentEditDistance.IncrComputePrefix(esi.Scores, prevEsi.Scores, word, prefixDiff, isLastWordComplete);
			foreach (EditOperation op in ops)
				esi.Operations.Add(op);
		}

		public void CorrectPrefix(IReadOnlyList<string> uncorrectedPrefix, IReadOnlyList<string> prefix, bool isLastWordComplete, IList<string> correctedPrefix,
			IList<Tuple<int, int>> sourceSegmentation, IList<int> targetSegmentCuts)
		{
			IReadOnlyList<EditOperation> wordOps, charOps;
			_segmentEditDistance.ComputePrefix(uncorrectedPrefix, prefix, isLastWordComplete, false, out wordOps, out charOps);
			CorrectPrefix(wordOps, charOps, uncorrectedPrefix, prefix, isLastWordComplete, correctedPrefix, sourceSegmentation, targetSegmentCuts);
		}

		private void CorrectPrefix(IReadOnlyList<EditOperation> wordOps, IReadOnlyList<EditOperation> charOps,
			IReadOnlyList<string> uncorrectedPrefix, IReadOnlyList<string> prefix, bool isLastWordComplete, IList<string> correctedPrefix,
			IList<Tuple<int, int>> sourceSegmentation, IList<int> targetSegmentCuts)
		{
			correctedPrefix.Clear();

			int i = 0, j = 0, k = 0;
			foreach (EditOperation wordOp in wordOps)
			{
				switch (wordOp)
				{
					case EditOperation.Insert:
						correctedPrefix.Add(prefix[j]);

						j++;
						for (int l = 0; l < targetSegmentCuts.Count; l++)
							targetSegmentCuts[l]++;
						break;

					case EditOperation.Delete:
						i++;
						if (k < targetSegmentCuts.Count)
						{
							for (int l = 0; l < targetSegmentCuts.Count; l++)
								targetSegmentCuts[l]--;
							if (targetSegmentCuts[k] == 0 || (k > 0 && targetSegmentCuts[k] == targetSegmentCuts[k - 1]))
							{
								sourceSegmentation.RemoveAt(k);
								targetSegmentCuts.RemoveAt(k);
							}
						}
						break;

					case EditOperation.Hit:
						if (j < prefix.Count - 1 || isLastWordComplete)
							correctedPrefix.Add(prefix[j]);
						else
							correctedPrefix.Add(CorrectWord(charOps, uncorrectedPrefix[i], prefix[j]));
						i++;
						j++;
						if (k < targetSegmentCuts.Count && correctedPrefix.Count >= targetSegmentCuts[k])
							k++;
						break;

					case EditOperation.Substitute:
						correctedPrefix.Add(prefix[j]);
						i++;
						j++;
						if (k < targetSegmentCuts.Count && correctedPrefix.Count >= targetSegmentCuts[k])
							k++;
						break;
				}
			}

			for (; i < uncorrectedPrefix.Count; i++)
				correctedPrefix.Add(uncorrectedPrefix[i]);
		}

		private string CorrectWord(IReadOnlyList<EditOperation> charOps, string word, string prefix)
		{
			var sb = new StringBuilder();
			int i = 0, j = 0;
			foreach (EditOperation charOp in charOps)
			{
				switch (charOp)
				{
					case EditOperation.Hit:
						sb.Append(word[i]);
						i++;
						j++;
						break;

					case EditOperation.Insert:
						sb.Append(prefix[j]);
						j++;
						break;

					case EditOperation.Delete:
						i++;
						break;

					case EditOperation.Substitute:
						sb.Append(prefix[j]);
						i++;
						j++;
						break;
				}
			}

			sb.Append(word.Substring(i));
			return sb.ToString();
		}

		public IReadOnlyList<int> GetLastInsPrefixWordFromEsi(EcmScoreInfo esi)
		{
			var results = new int[esi.Operations.Count];

			for (int j = esi.Operations.Count - 1; j >= 0; j--)
			{
				switch (esi.Operations[j])
				{
					case EditOperation.Hit:
						results[j] = j - 1;
						break;

					case EditOperation.Insert:
						int tj = j;
						while (tj >= 0 && esi.Operations[tj] == EditOperation.Insert)
							tj--;
						if (esi.Operations[tj] == EditOperation.Hit || esi.Operations[tj] == EditOperation.Substitute)
							tj--;
						results[j] = tj;
						break;

					case EditOperation.Delete:
						results[j] = j;
						break;

					case EditOperation.Substitute:
						results[j] = j - 1;
						break;

					case EditOperation.None:
						results[j] = 0;
						break;
				}
			}

			return results;
		}
	}
}
