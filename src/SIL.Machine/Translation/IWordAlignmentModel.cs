using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
    public interface IWordAlignmentModel : IWordAlignmentEngine
    {
        ITrainer CreateTrainer(IParallelTextCorpus corpus);
    }
}
