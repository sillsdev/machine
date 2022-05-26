namespace SIL.Machine.WebApi.Services;

public interface ISmtModelFactory
{
    IInteractiveTranslationModel Create(string engineId);
    ITrainer CreateTrainer(string engineId, IParallelTextCorpus corpus);
    void InitNew(string engineId);
    void Cleanup(string engineId);
}
