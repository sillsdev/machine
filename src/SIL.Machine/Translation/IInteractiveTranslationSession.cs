using System;
using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public interface IInteractiveTranslationSession : IDisposable
	{
		ReadOnlyList<string> SourceSegment { get; }

		ReadOnlyList<string> Prefix { get; }

		bool IsLastWordPartial { get; }

		TranslationResult TranslateInteractively(IEnumerable<string> segment);

		TranslationResult AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial);

		TranslationResult SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial);

		void Reset();

		void Approve();
	}
}
