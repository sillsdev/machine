using System;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
    public interface ITranslationModel : IDisposable
    {
        ITranslationEngine CreateEngine();
        ITrainer CreateTrainer(IParallelTextCorpus corpus);
    }
}
