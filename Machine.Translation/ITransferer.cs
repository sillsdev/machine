using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ITransferer
	{
		IEnumerable<WordAnalysis> Transfer(WordAnalysis sourceAnalysis);
	}
}
