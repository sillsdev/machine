using System.Collections.Generic;
using SIL.Progress;

namespace SIL.Machine.Translation.Thot
{
	public interface ILLWeightTuner
	{
		IReadOnlyList<double> Tune(string cfgFileName, IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus, IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus,
			IReadOnlyList<double> initialWeights, IProgress progress);
	}
}
