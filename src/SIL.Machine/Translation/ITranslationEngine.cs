using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ITranslationEngine : IDisposable
	{
		TranslationResult Translate(IEnumerable<string> segment);

		IEnumerable<TranslationResult> Translate(int n, IEnumerable<string> segment);
	}
}
