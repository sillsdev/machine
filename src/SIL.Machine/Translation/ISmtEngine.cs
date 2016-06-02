using System.Collections.Generic;
using SIL.Progress;

namespace SIL.Machine.Translation
{
	public interface ISmtEngine : ITranslationEngine
	{
		void Save();

		void Train(IEnumerable<IEnumerable<string>> sourceCorpus, IEnumerable<IEnumerable<string>> targetCorpus, IProgress progress = null);
	}
}
