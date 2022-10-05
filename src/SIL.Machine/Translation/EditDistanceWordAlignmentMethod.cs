using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.SequenceAlignment;

namespace SIL.Machine.Translation
{
    public class EditDistanceWordAlignmentMethod : WordAlignerBase, IWordAlignmentMethod
    {
        private Func<IReadOnlyList<string>, int, IReadOnlyList<string>, int, double> _scoreSelector;
        private SegmentScorer _scorer;

        public Func<IReadOnlyList<string>, int, IReadOnlyList<string>, int, double> ScoreSelector
        {
            get => _scoreSelector;
            set
            {
                _scoreSelector = value;
                _scorer = _scoreSelector == null ? null : new SegmentScorer(_scoreSelector);
            }
        }

        public override WordAlignmentMatrix Align(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment
        )
        {
            if (_scorer == null)
                throw new InvalidOperationException("A score selector has not been assigned.");

            var paa = new PairwiseAlignmentAlgorithm<IReadOnlyList<string>, int>(
                _scorer,
                sourceSegment,
                targetSegment,
                GetWordIndices
            )
            {
                Mode = AlignmentMode.Global,
                ExpansionCompressionEnabled = true,
                TranspositionEnabled = true
            };
            paa.Compute();
            Alignment<IReadOnlyList<string>, int> alignment = paa.GetAlignments().First();
            var waMatrix = new WordAlignmentMatrix(sourceSegment.Count, targetSegment.Count);
            for (int c = 0; c < alignment.ColumnCount; c++)
            {
                foreach (int i in alignment[0, c])
                {
                    foreach (int j in alignment[1, c])
                        waMatrix[i, j] = true;
                }
            }

            return waMatrix;
        }

        private static IEnumerable<int> GetWordIndices(IReadOnlyList<string> sequence, out int index, out int count)
        {
            index = 0;
            count = sequence.Count;
            return Enumerable.Range(index, count);
        }
    }
}
