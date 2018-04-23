namespace SIL.Machine.Translation
{
	public interface IInteractiveSmtModel : ISmtModel
	{
		IInteractiveSmtEngine CreateInteractiveEngine();
	}
}
