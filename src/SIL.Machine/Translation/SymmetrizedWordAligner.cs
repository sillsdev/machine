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

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			WordAlignmentMatrix matrix = _srcTrgAligner.GetBestAlignment(sourceSegment, targetSegment);
			WordAlignmentMatrix invMatrix = _trgSrcAligner.GetBestAlignment(targetSegment, sourceSegment);

			invMatrix.Transpose();
			switch (Heuristic)
			{
				case SymmetrizationHeuristic.Union:
					matrix.UnionWith(invMatrix);
					break;
				case SymmetrizationHeuristic.Intersection:
					matrix.IntersectWith(invMatrix);
					break;
				case SymmetrizationHeuristic.Och:
					matrix.SymmetrizeWith(invMatrix);
					break;
				case SymmetrizationHeuristic.Grow:
					matrix.GrowSymmetrizeWith(invMatrix);
					break;
				case SymmetrizationHeuristic.GrowDiag:
					matrix.GrowDiagSymmetrizeWith(invMatrix);
					break;
				case SymmetrizationHeuristic.GrowDiagFinal:
					matrix.GrowDiagFinalSymmetrizeWith(invMatrix);
					break;
				case SymmetrizationHeuristic.GrowDiagFinalAnd:
					matrix.GrowDiagFinalAndSymmetrizeWith(invMatrix);
					break;
			}
			return matrix;
		}
	}
}
