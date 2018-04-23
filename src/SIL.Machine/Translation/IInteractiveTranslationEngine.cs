using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IInteractiveTranslationEngine : ITranslationEngine
	{
		IInteractiveTranslationSession TranslateInteractively(int n, IReadOnlyList<string> segment);
	}
}
