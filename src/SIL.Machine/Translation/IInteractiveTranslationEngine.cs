using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IInteractiveTranslationEngine : ITranslationEngine
	{
		IInteractiveTranslationSession TranslateInteractively(IReadOnlyList<string> segment);
	}
}
