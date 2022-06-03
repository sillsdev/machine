using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
    public static class Evaluation
    {
        private const int BleuN = 4;

        public static double ComputeBleu(
            IEnumerable<IReadOnlyList<string>> translations,
            IEnumerable<IReadOnlyList<string>> references
        )
        {
            var precs = new double[BleuN];
            var total = new double[BleuN];
            int transWordCount = 0,
                refWordCount = 0;

            foreach (var (translation, reference) in translations.Zip(references, (t, r) => (t, r)))
            {
                transWordCount += translation.Count;
                refWordCount += reference.Count;
                for (int n = 1; n <= BleuN; n++)
                {
                    ComputeBleuPrecision(translation, reference, n, out int segPrec, out int segTotal);
                    precs[n - 1] += segPrec;
                    total[n - 1] += segTotal;
                }
            }

            double brevityPenalty =
                transWordCount < refWordCount ? Math.Exp(1.0 - ((double)refWordCount / transWordCount)) : 1.0;

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

        private static void ComputeBleuPrecision(
            IReadOnlyList<string> translation,
            IReadOnlyList<string> reference,
            int n,
            out int prec,
            out int total
        )
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

        public static double ComputeAer(
            IEnumerable<IReadOnlyCollection<AlignedWordPair>> alignments,
            IEnumerable<IReadOnlyCollection<AlignedWordPair>> references
        )
        {
            (int aCount, int sCount, int paCount, int saCount) = GetAlignmentCounts(alignments, references);
            return 1 - ((double)(paCount + saCount) / (sCount + aCount));
        }

        public static (double FScore, double Precision, double Recall) ComputeAlignmentFScore(
            IEnumerable<IReadOnlyCollection<AlignedWordPair>> alignments,
            IEnumerable<IReadOnlyCollection<AlignedWordPair>> references,
            double alpha = 0.5
        )
        {
            (int aCount, int sCount, int paCount, int saCount) = GetAlignmentCounts(alignments, references);
            double precision = (double)paCount / aCount;
            double recall = (double)saCount / sCount;
            double fScore = 1 / ((alpha / precision) + ((1 - alpha) / recall));
            return (fScore, precision, recall);
        }

        private static (int ACount, int SCount, int PACount, int SACount) GetAlignmentCounts(
            IEnumerable<IReadOnlyCollection<AlignedWordPair>> alignments,
            IEnumerable<IReadOnlyCollection<AlignedWordPair>> references
        )
        {
            int aCount = 0;
            int sCount = 0;
            int paCount = 0;
            int saCount = 0;
            foreach (var (alignment, reference) in alignments.Zip(references, (a, r) => (a, r)))
            {
                aCount += alignment.Count;
                foreach (AlignedWordPair wp in reference)
                {
                    if (wp.IsSure)
                    {
                        sCount++;
                        if (alignment.Contains(wp))
                        {
                            saCount++;
                            paCount++;
                        }
                    }
                    else if (alignment.Contains(wp))
                    {
                        paCount++;
                    }
                }
            }
            return (aCount, sCount, paCount, saCount);
        }
    }
}
