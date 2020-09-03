namespace SIL.Machine.Translation
{
	public interface IInteractiveTranslationModel : ITranslationModel
	{
		IInteractiveTranslationEngine CreateInteractiveEngine();
	}
}
