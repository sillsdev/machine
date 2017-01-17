using SIL.Machine.Translation;

namespace SIL.Machine.WebApi.Models
{
	public interface ITranslationEngineFactory
	{
		ITranslationEngine Create(EngineContext engineContext);
	}
}
