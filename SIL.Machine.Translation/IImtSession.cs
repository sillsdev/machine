using System;
using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public interface IImtSession : IDisposable
	{
		IReadOnlyList<string> SourceSegment { get; }

		IReadOnlyList<string> Prefix { get; }

		bool IsLastWordPartial { get; }

		TranslationResult TranslateInteractively(IEnumerable<string> segment);

		TranslationResult AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial);

		TranslationResult SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial);

		void Reset();

		void Approve();

		TranslationResult Translate(IEnumerable<string> segment);
	}
}
