using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IInteractiveTranslationEngine : ITranslationEngine
	{
		IInteractiveTranslationSession TranslateInteractively(IEnumerable<string> segment);
	}
}
