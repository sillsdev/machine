using System.Collections.Generic;
using SIL.Progress;

namespace SIL.Machine.Translation.Thot
{
	public interface ILLWeightTuner
	{
		double[] Tune(string cfgFileName, IList<IList<string>> tuneSourceCorpus, IList<IList<string>> tuneTargetCorpus, double[] initialWeights, IProgress progress = null);
	}
}
