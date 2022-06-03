using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Morphology;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
    public class TransferResult
    {
        public TransferResult(IEnumerable<WordAnalysis> analyses, WordAlignmentMatrix alignment)
        {
            TargetAnalyses = new ReadOnlyList<WordAnalysis>(analyses.ToArray());
            WordAlignmentMatrix = alignment;
        }

        public IReadOnlyList<WordAnalysis> TargetAnalyses { get; }

        public WordAlignmentMatrix WordAlignmentMatrix { get; }
    }
}
