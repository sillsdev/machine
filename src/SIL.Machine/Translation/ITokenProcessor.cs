using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ITokenProcessor
	{
		IReadOnlyList<string> Process(IReadOnlyList<string> tokens);
	}
}
