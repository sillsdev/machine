using System;
using System.Collections.Generic;
using SIL.Progress;

namespace SIL.Machine.Translation
{
	public interface ISmtEngine : IDisposable
	{
		ISmtSession StartSession();

		void SaveModels();

		void Train(IEnumerable<IEnumerable<string>> sourceCorpus, IEnumerable<IEnumerable<string>> targetCorpus, IProgress progress = null);
	}
}
