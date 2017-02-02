using System.Collections.Generic;
using SIL.Progress;

namespace SIL.Machine.Translation.Thot
{
	public interface IParameterTuner
	{
		ThotSmtParameters Tune(string tmFileNamePrefix, string lmFileNamePrefix, ThotSmtParameters parameters, IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus,
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus, IProgress progress);
	}
}
