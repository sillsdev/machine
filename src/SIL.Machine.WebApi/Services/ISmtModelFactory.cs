namespace SIL.Machine.WebApi.Services;

public interface ISmtModelFactory
{
	IInteractiveTranslationModel Create(string engineId);
	void InitNew(string engineId);
	void Cleanup(string engineId);
}
