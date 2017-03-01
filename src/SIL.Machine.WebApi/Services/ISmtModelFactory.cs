using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Services
{
	public interface ISmtModelFactory
	{
		IInteractiveSmtModel Create(Engine engine);
	}
}
