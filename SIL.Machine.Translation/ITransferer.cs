using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ITransferer
	{
		IEnumerable<WordAnalysis> Transfer(IEnumerable<IEnumerable<WordAnalysis>> sourceAnalyses, out WordAlignmentMatrix waMatrix);
	}
}
