using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Models
{
	public interface ISmtModelFactory
	{
		IInteractiveSmtModel Create(EngineContext engineContext);
	}
}
