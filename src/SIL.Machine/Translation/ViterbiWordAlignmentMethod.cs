using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
    public class ViterbiWordAlignmentMethod : IWordAlignmentMethod
    {
        public Func<IReadOnlyList<string>, int, IReadOnlyList<string>, int, double> ScoreSelector { get; set; }

        public WordAlignmentMatrix Align(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
        {
            var alignment = new WordAlignmentMatrix(sourceSegment.Count, targetSegment.Count);
            for (int j = 0; j < targetSegment.Count; j++)
            {
                int bestI = -1;
                double bestScore = 0;
                for (int i = 0; i < sourceSegment.Count; i++)
                {
                    double score = ScoreSelector(sourceSegment, i, targetSegment, j);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestI = i;
                    }
                }

                if (bestI != -1)
                    alignment[bestI, j] = true;
            }
            return alignment;
        }
    }
}
