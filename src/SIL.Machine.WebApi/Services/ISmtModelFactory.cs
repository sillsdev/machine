namespace SIL.Machine.WebApi.Services;

public interface ISmtModelFactory
{
	IInteractiveTranslationModel Create(string engineId);
	ITrainer CreateTrainer(string engineId, ParallelTextCorpus corpus, ITokenProcessor sourcePreprocessor,
		ITokenProcessor targetPreprocessor);
	void InitNew(string engineId);
	void Cleanup(string engineId);
}
