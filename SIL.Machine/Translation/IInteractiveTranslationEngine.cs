namespace SIL.Machine.Translation
{
	public interface IInteractiveTranslationEngine : ITranslationEngine
	{
		IInteractiveTranslationSession StartSession();
	}
}
