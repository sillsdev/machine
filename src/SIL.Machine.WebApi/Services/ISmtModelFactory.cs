using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Services
{
	public interface ISmtModelFactory
	{
		IInteractiveSmtModel Create(string configDir);
		void InitNewModel(string configDir);
	}
}
