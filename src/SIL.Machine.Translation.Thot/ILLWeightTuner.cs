using System.Collections.Generic;
using SIL.Progress;

namespace SIL.Machine.Translation.Thot
{
	public interface ILLWeightTuner
	{
		IReadOnlyList<float> Tune(string tmFileNamePrefix, string lmFileNamePrefix, ThotSmtParameters parameters, IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus,
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus, IReadOnlyList<float> initialWeights, IProgress progress);
	}
}
