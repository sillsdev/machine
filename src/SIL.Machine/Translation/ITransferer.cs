using System.Collections.Generic;
using SIL.Machine.Morphology;

namespace SIL.Machine.Translation
{
    public interface ITransferer
    {
        IEnumerable<TransferResult> Transfer(IEnumerable<IEnumerable<WordAnalysis>> sourceAnalyses);
    }
}
