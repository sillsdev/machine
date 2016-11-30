using System;
using System.Collections.Generic;
using SIL.Extensions;

namespace SIL.Machine.Translation
{
	public static class Evaluation
	{
		private const int BleuN = 4;

		public static double CalculateBleu(IEnumerable<IReadOnlyList<string>> translations, IEnumerable<IReadOnlyList<string>> references)
		{
			var precs = new double[BleuN];
			var total = new double[BleuN];
			int transWordCount = 0, refWordCount = 0;

			foreach (Tuple<IReadOnlyList<string>, IReadOnlyList<string>> pair in translations.Zip(references))
			{
				transWordCount += pair.Item1.Count;
				refWordCount += pair.Item2.Count;
				for (int n = 1; n <= BleuN; n++)
				{
					int segPrec, segTotal;
					CalculatePrecision(pair.Item1, pair.Item2, n, out segPrec, out segTotal);
					precs[n - 1] += segPrec;
					total[n - 1] += segTotal;
				}
			}

			double brevityPenalty = transWordCount < refWordCount ? Math.Exp(1.0 - ((double) refWordCount / transWordCount)) : 1.0;

			double bleu = 0;
			var bleus = new double[BleuN];
			for (int n = 1; n <= BleuN; n++)
			{
				bleus[n - 1] = total[n - 1] == 0 ? 0 : precs[n - 1] / total[n - 1];
				bleu += (1.0 / BleuN) * (bleus[n - 1] == 0 ? -999999999 : Math.Log(bleus[n - 1]));
			}
			bleu = brevityPenalty * Math.Exp(bleu);
			return bleu;
		}

		private static void CalculatePrecision(IReadOnlyList<string> translation, IReadOnlyList<string> reference, int n, out int prec, out int total)
		{
			total = n > translation.Count ? 0 : translation.Count - n + 1;
			int refTotal = n > reference.Count ? 0 : reference.Count - n + 1;

			var matched = new HashSet<int>();

			prec = 0;
			for (int i = 0; i < total; i++)
			{
				for (int j = 0; j < refTotal; j++)
				{
					bool match = true;
					for (int k = 0; k < n; k++)
					{
						if (translation[i + k] != reference[j + k])
						{
							match = false;
							break;
						}
					}

					if (match && !matched.Contains(j))
					{
						prec++;
						matched.Add(j);
						break;
					}
				}
			}
		}
	}
}
