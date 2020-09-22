using System;

namespace SIL.Machine.Translation.Thot
{
	internal interface IThotSmtModelInternal
	{
		IntPtr Handle { get; }
		ThotSmtParameters Parameters { get; }

		double GetTranslationProbability(string sourceWord, string targetWord);
		void RemoveEngine(ThotSmtEngine engine);
	}
}
