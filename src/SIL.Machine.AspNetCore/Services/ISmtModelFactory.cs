namespace SIL.Machine.AspNetCore.Services;

public interface ISmtModelFactory
{
    IInteractiveTranslationModel Create(string engineId);
    ITrainer CreateTrainer(string engineId, IParallelTextCorpus corpus);
    void InitNew(string engineId);
    void Cleanup(string engineId);
}
