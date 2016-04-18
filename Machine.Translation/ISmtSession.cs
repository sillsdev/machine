using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ISmtSession : IDisposable
	{
		IEnumerable<string> Translate(IEnumerable<string> segment);

		IEnumerable<string> TranslateInteractively(IEnumerable<string> segment);

		IEnumerable<string> AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial);

		IEnumerable<string> SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial);

		void Train(IEnumerable<string> sourceSentence, IEnumerable<string> targetSentence);
	}
}
