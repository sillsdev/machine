using System;
using System.Collections.Generic;
using System.Linq;

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

        public void SetErrorModelParameters(
            int vocSize,
            double hitProb,
            double insFactor,
            double substFactor,
            double delFactor
        )
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

        public void ExtendEsi(
            EcmScoreInfo esi,
            EcmScoreInfo prevEsi,
            string word,
            string[] prefixDiff,
            bool isLastWordComplete
        )
        {
            IEnumerable<EditOperation> ops = _segmentEditDistance.IncrComputePrefix(
                esi.Scores,
                prevEsi.Scores,
                word,
                prefixDiff,
                isLastWordComplete
            );
            foreach (EditOperation op in ops)
                esi.Operations.Add(op);
        }

        public int CorrectPrefix(
            TranslationResultBuilder builder,
            int uncorrectedPrefixLen,
            string[] prefix,
            bool isLastWordComplete
        )
        {
            if (uncorrectedPrefixLen == 0)
            {
                foreach (string w in prefix)
                    builder.AppendToken(w, TranslationSources.Prefix, -1);
                return prefix.Length;
            }

            IEnumerable<EditOperation> wordOps,
                charOps;
            _segmentEditDistance.ComputePrefix(
                builder.TargetTokens.Take(uncorrectedPrefixLen).ToArray(),
                prefix,
                isLastWordComplete,
                false,
                out wordOps,
                out charOps
            );
            return builder.CorrectPrefix(wordOps, charOps, prefix, isLastWordComplete);
        }
    }
}
