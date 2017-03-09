using SIL.Machine.Translation;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Services
{
	public interface ITranslationEngineFactory
	{
		ITranslationEngine Create(Engine engine);
	}
}
