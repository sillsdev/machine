using System;
using System.Collections.Generic;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation.Thot
{
    public interface IParameterTuner
    {
        ThotSmtParameters Tune(
            ThotSmtParameters parameters,
            IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus,
            IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus,
            TrainStats stats,
            IProgress<ProgressStatus> progress
        );
    }
}
