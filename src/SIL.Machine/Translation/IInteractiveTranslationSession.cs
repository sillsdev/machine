using System;
using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public interface IInteractiveTranslationSession : IDisposable
	{
		ReadOnlyList<string> SourceSegment { get; }

		ReadOnlyList<string> Prefix { get; }

		bool IsLastWordComplete { get; }

		TranslationResult CurrenTranslationResult { get; }

		TranslationResult TranslateInteractively(IEnumerable<string> segment);

		TranslationResult AddToPrefix(IEnumerable<string> addition, bool isLastWordComplete);

		TranslationResult SetPrefix(IEnumerable<string> prefix, bool isLastWordComplete);

		void Reset();

		void Approve();
	}
}
