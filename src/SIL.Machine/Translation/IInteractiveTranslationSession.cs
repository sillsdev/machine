using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IInteractiveTranslationSession : IDisposable
	{
		IReadOnlyList<string> SourceSegment { get; }

		IReadOnlyList<string> Prefix { get; }

		bool IsLastWordComplete { get; }

		TranslationResult CurrentResult { get; }

		TranslationResult SetPrefix(IReadOnlyList<string> prefix, bool isLastWordComplete);

		TranslationResult AppendToPrefix(string addition, bool isLastWordComplete);

		TranslationResult AppendToPrefix(IEnumerable<string> words);

		void Approve();
	}
}
