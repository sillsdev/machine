using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Services
{
	public interface IRuleEngineFactory
	{
		ITranslationEngine Create(string engineId);
		void InitNewEngine(string engineId);
		void CleanupEngine(string engineId);
	}
}
