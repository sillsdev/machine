using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ITranslator
	{
		TranslationResult Translate(IEnumerable<string> segment);
	}
}
