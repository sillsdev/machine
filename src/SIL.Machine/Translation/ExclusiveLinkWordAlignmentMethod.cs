using System;
using System.Collections.Generic;
using SIL.Machine.DataStructures;

namespace SIL.Machine.Translation
{
    public class ExclusiveLinkWordAlignmentMethod : IWordAlignmentMethod
    {
        public Func<IReadOnlyList<string>, int, IReadOnlyList<string>, int, double> ScoreSelector { get; set; }

        public WordAlignmentMatrix GetBestAlignment(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment
        )
        {
            var queue = new PriorityQueue<WordPair>(sourceSegment.Count * targetSegment.Count);
            for (int j = 0; j < targetSegment.Count; j++)
            {
                for (int i = 0; i < sourceSegment.Count; i++)
                {
                    double score = ScoreSelector(sourceSegment, i, targetSegment, j);
                    queue.Enqueue(new WordPair(score, i, j));
                }
            }

            var alignment = new WordAlignmentMatrix(sourceSegment.Count, targetSegment.Count);
            int alignedCount = 0;
            var alignedSourceIndices = new HashSet<int>();
            var alignedTargetIndices = new HashSet<int>();
            while (alignedCount < sourceSegment.Count && alignedCount < targetSegment.Count)
            {
                WordPair candidate = queue.Dequeue();
                if (
                    !alignedSourceIndices.Contains(candidate.SourceIndex)
                    && !alignedTargetIndices.Contains(candidate.TargetIndex)
                )
                {
                    alignment[candidate.SourceIndex, candidate.TargetIndex] = true;
                    alignedSourceIndices.Add(candidate.SourceIndex);
                    alignedTargetIndices.Add(candidate.TargetIndex);
                    alignedCount++;
                }
            }
            return alignment;
        }

        private class WordPair : PriorityQueueNodeBase, IComparable<WordPair>
        {
            public WordPair(double score, int sourceIndex, int targetIndex)
            {
                Score = score;
                SourceIndex = sourceIndex;
                TargetIndex = targetIndex;
            }

            public double Score { get; }
            public int SourceIndex { get; }
            public int TargetIndex { get; }
            public int IndexDistance => Math.Abs(TargetIndex - SourceIndex);

            public int CompareTo(WordPair other)
            {
                int result = -Score.CompareTo(other.Score);
                if (result != 0)
                    return result;
                return IndexDistance.CompareTo(other.IndexDistance);
            }
        }
    }
}
