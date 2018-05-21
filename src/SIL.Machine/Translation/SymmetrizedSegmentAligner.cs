using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public class SymmetrizedSegmentAligner : ISegmentAligner
	{
		private readonly ISegmentAligner _srcTrgAligner;
		private readonly ISegmentAligner _trgSrcAligner;

		public SymmetrizedSegmentAligner(ISegmentAligner srcTrgAligner, ISegmentAligner trgSrcAligner)
		{
			_srcTrgAligner = srcTrgAligner;
			_trgSrcAligner = trgSrcAligner;
		}

		public WordAlignmentMatrix GetBestAlignment(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			WordAlignmentMatrix matrix = _srcTrgAligner.GetBestAlignment(sourceSegment, targetSegment);
			WordAlignmentMatrix invMatrix = _trgSrcAligner.GetBestAlignment(targetSegment, sourceSegment);

			invMatrix.Transpose();
			matrix.SymmetrizeWith(invMatrix);
			return matrix;
		}
	}
}
