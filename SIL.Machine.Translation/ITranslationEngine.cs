using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ITranslationEngine
	{
		TranslationResult Translate(IEnumerable<string> segment);
	}
}
