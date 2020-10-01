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
