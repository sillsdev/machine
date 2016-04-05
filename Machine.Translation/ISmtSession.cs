using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public interface ISmtSession : IDisposable
	{
		SmtResult Translate(IEnumerable<string> segment);

		SmtResult TranslateInteractively(IEnumerable<string> segment);

		SmtResult AddToPrefix(IEnumerable<string> addition, bool isLastWordPartial);

		SmtResult SetPrefix(IEnumerable<string> prefix, bool isLastWordPartial);

		void Train(IEnumerable<string> sourceSentence, IEnumerable<string> targetSentence);
	}
}
