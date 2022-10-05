using System.Collections.Generic;

namespace SIL.Machine.Translation
{
    public class SymmetrizedWordAligner : IWordAligner
    {
        private readonly IWordAligner _srcTrgAligner;
        private readonly IWordAligner _trgSrcAligner;

        public SymmetrizedWordAligner(IWordAligner srcTrgAligner, IWordAligner trgSrcAligner)
        {
            _srcTrgAligner = srcTrgAligner;
            _trgSrcAligner = trgSrcAligner;
        }

        public SymmetrizationHeuristic Heuristic { get; set; } = SymmetrizationHeuristic.Och;

        public WordAlignmentMatrix Align(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
        {
            WordAlignmentMatrix matrix = _srcTrgAligner.Align(sourceSegment, targetSegment);
            if (Heuristic != SymmetrizationHeuristic.None)
            {
                WordAlignmentMatrix invMatrix = _trgSrcAligner.Align(targetSegment, sourceSegment);

                invMatrix.Transpose();
                matrix.SymmetrizeWith(invMatrix, Heuristic);
            }
            return matrix;
        }
    }
}
