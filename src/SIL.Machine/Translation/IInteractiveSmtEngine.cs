namespace SIL.Machine.Translation
{
	public interface IInteractiveSmtEngine : IInteractiveTranslationEngine, ISmtEngine
	{
		new IInteractiveSmtSession StartSession();
	}
}
