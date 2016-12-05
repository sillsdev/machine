using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface IInteractiveTranslationSession : IDisposable
	{
		IReadOnlyList<string> SourceSegment { get; }

		IReadOnlyList<string> Prefix { get; }

		bool IsLastWordComplete { get; }

		TranslationResult CurrenTranslationResult { get; }

		TranslationResult AddToPrefix(IEnumerable<string> addition, bool isLastWordComplete);

		TranslationResult SetPrefix(IEnumerable<string> prefix, bool isLastWordComplete);

		void Approve();
	}
}
