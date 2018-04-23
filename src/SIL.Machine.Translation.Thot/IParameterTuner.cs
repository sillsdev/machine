using System.Collections.Generic;

namespace SIL.Machine.Translation.Thot
{
	public interface IParameterTuner
	{
		ThotSmtParameters Tune(ThotSmtParameters parameters, IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus,
			IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus, ThotTrainProgressReporter reporter,
			SmtBatchTrainStats stats);
	}
}
