using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.Machine.Translation
{
	public class ErrorCorrectionModel
	{
		private readonly SegmentEditDistance _segmentEditDistance;

		public ErrorCorrectionModel()
		{
			_segmentEditDistance = new SegmentEditDistance();
			SetErrorModelParameters(128, 0.8, 1, 1, 1);
		}

		public void SetErrorModelParameters(int vocSize, double hitProb, double insFactor, double substFactor,
			double delFactor)
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
			double score = _segmentEditDistance.Compute(new string[] { word }, new string[0]);
			esi.Scores.Clear();
			esi.Scores.Add(prevEsi.Scores[0] + score);
			esi.Operations.Clear();
			esi.Operations.Add(EditOperation.None);
		}

		public void ExtendInitialEsi(EcmScoreInfo initialEsi, EcmScoreInfo prevInitialEsi, string[] prefixDiff)
		{
			_segmentEditDistance.IncrComputePrefixFirstRow(initialEsi.Scores, prevInitialEsi.Scores, prefixDiff);
		}

		public void ExtendEsi(EcmScoreInfo esi, EcmScoreInfo prevEsi, string word, string[] prefixDiff,
			bool isLastWordComplete)
		{
			IEnumerable<EditOperation> ops = _segmentEditDistance.IncrComputePrefix(esi.Scores, prevEsi.Scores, word,
				prefixDiff, isLastWordComplete);
			foreach (EditOperation op in ops)
				esi.Operations.Add(op);
		}

		public int CorrectPrefix(TranslationInfo correction, int uncorrectedPrefixLen, string[] prefix,
			bool isLastWordComplete)
		{
			if (uncorrectedPrefixLen == 0)
			{
				foreach (string w in prefix)
				{
					correction.Target.Add(w);
					correction.TargetConfidences.Add(-1);
				}
				return prefix.Length;
			}

			IEnumerable<EditOperation> wordOps, charOps;
			_segmentEditDistance.ComputePrefix(correction.Target.Take(uncorrectedPrefixLen).ToArray(), prefix,
				isLastWordComplete, false, out wordOps, out charOps);
			return CorrectPrefix(correction, wordOps, charOps, prefix, isLastWordComplete);
		}

		private int CorrectPrefix(TranslationInfo correction, IEnumerable<EditOperation> wordOps,
			IEnumerable<EditOperation> charOps, string[] prefix, bool isLastWordComplete)
		{
			var alignmentColsToCopy = new List<int>();

			int i = 0, j = 0, k = 0;
			foreach (EditOperation wordOp in wordOps)
			{
				switch (wordOp)
				{
					case EditOperation.Insert:
						correction.Target.Insert(j, prefix[j]);
						correction.TargetConfidences.Insert(j, -1);
						alignmentColsToCopy.Add(-1);
						for (int l = k; l < correction.Phrases.Count; l++)
							correction.Phrases[l].TargetCut++;
						j++;
						break;

					case EditOperation.Delete:
						correction.Target.RemoveAt(j);
						correction.TargetConfidences.RemoveAt(j);
						i++;
						if (k < correction.Phrases.Count)
						{
							for (int l = k; l < correction.Phrases.Count; l++)
								correction.Phrases[l].TargetCut--;

							if (correction.Phrases[k].TargetCut < 0
								|| (k > 0 && correction.Phrases[k].TargetCut == correction.Phrases[k - 1].TargetCut))
							{
								correction.Phrases.RemoveAt(k);
								alignmentColsToCopy.Clear();
								i = 0;
							}
							else if (j > correction.Phrases[k].TargetCut)
							{
								ResizeAlignment(correction, k, alignmentColsToCopy);
								alignmentColsToCopy.Clear();
								i = 0;
								k++;
							}
						}
						break;

					case EditOperation.Hit:
					case EditOperation.Substitute:
						if (wordOp == EditOperation.Substitute || j < prefix.Length - 1 || isLastWordComplete)
							correction.Target[j] = prefix[j];
						else
							correction.Target[j] = CorrectWord(charOps, correction.Target[j], prefix[j]);

						if (wordOp == EditOperation.Substitute)
							correction.TargetConfidences[j] = -1;
						else if (wordOp == EditOperation.Hit)
							correction.TargetUncorrectedPrefixWords.Add(j);

						alignmentColsToCopy.Add(i);

						i++;
						j++;
						if (k < correction.Phrases.Count && j > correction.Phrases[k].TargetCut)
						{
							ResizeAlignment(correction, k, alignmentColsToCopy);
							alignmentColsToCopy.Clear();
							i = 0;
							k++;
						}
						break;
				}
			}

			while (j < correction.Target.Count)
			{
				alignmentColsToCopy.Add(i);

				i++;
				j++;
				if (k < correction.Phrases.Count && j > correction.Phrases[k].TargetCut)
				{
					ResizeAlignment(correction, k, alignmentColsToCopy);
					alignmentColsToCopy.Clear();
					break;
				}
			}

			return alignmentColsToCopy.Count;
		}

		private void ResizeAlignment(TranslationInfo correction, int phraseIndex, List<int> colsToCopy)
		{
			WordAlignmentMatrix curAlignment = correction.Phrases[phraseIndex].Alignment;
			if (colsToCopy.Count == curAlignment.ColumnCount)
				return;

			var newAlignment = new WordAlignmentMatrix(curAlignment.RowCount, colsToCopy.Count);
			for (int j = 0; j < newAlignment.ColumnCount; j++)
			{
				if (colsToCopy[j] != -1)
				{
					for (int i = 0; i < newAlignment.RowCount; i++)
						newAlignment[i, j] = curAlignment[i, colsToCopy[j]];
				}
			}

			correction.Phrases[phraseIndex].Alignment = newAlignment;
		}

		private string CorrectWord(IEnumerable<EditOperation> charOps, string word, string prefix)
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
	}
}
