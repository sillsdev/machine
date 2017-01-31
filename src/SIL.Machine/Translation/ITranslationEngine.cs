using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ITranslationEngine : IDisposable
	{
		TranslationResult Translate(IReadOnlyList<string> segment);

		IEnumerable<TranslationResult> Translate(int n, IReadOnlyList<string> segment);
	}
}
