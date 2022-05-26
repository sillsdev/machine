using System.Collections.Generic;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;

namespace SIL.Machine.Plugin
{
    public interface IWordAlignmentModelFactory
    {
        string ModelType { get; }
        bool IsSymmetric { get; }

        IWordAlignmentModel CreateModel(
            string modelPath,
            WordAlignmentDirection direction,
            SymmetrizationHeuristic symHeuristic
        );
        ITrainer CreateTrainer(
            string modelPath,
            IEnumerable<ParallelTextRow> parallelCorpus,
            int maxCorpusCount,
            Dictionary<string, string> parameters,
            bool direct = true
        );
    }
}
