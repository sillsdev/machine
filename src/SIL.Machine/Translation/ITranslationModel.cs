using System;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation
{
    public interface ITranslationModel : ITranslationEngine, IDisposable
    {
        ITrainer CreateTrainer(IParallelTextCorpus corpus);
    }
}
