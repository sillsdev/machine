using System.Collections.Generic;

namespace SIL.Machine.Corpora
{
	public interface ITokenProcessor
	{
		IReadOnlyList<string> Process(IReadOnlyList<string> tokens);
	}
}
