using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Services
{
	public interface ISmtModelFactory
	{
		IInteractiveSmtModel Create(string engineId);
		void InitNewModel(string engineId);
		void CleanupModel(string engineId);
	}
}
