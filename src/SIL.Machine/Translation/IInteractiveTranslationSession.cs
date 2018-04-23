using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IInteractiveTranslationSession : IDisposable
	{
		IReadOnlyList<string> SourceSegment { get; }

		IReadOnlyList<string> Prefix { get; }

		bool IsLastWordComplete { get; }

		IReadOnlyList<TranslationResult> CurrentResults { get; }

		IReadOnlyList<TranslationResult> SetPrefix(IReadOnlyList<string> prefix, bool isLastWordComplete);

		IReadOnlyList<TranslationResult> AppendToPrefix(string addition, bool isLastWordComplete);

		IReadOnlyList<TranslationResult> AppendToPrefix(IEnumerable<string> words);

		void Approve();
	}
}
