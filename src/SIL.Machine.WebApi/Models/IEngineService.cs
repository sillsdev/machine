using System.Collections.Generic;

namespace SIL.Machine.WebApi.Models
{
	public interface IEngineService
	{
		IEnumerable<EngineContext> GetAll();
		bool TryGet(string sourceLanguageTag, string targetLanguageTag, out EngineContext engineContext);
		bool TryCreateSession(string sourceLanguageTag, string targetLanguageTag, out SessionContext sessionContext);
		bool TryTranslate(string sourceLanguageTag, string targetLanguageTag, string segment, out string result);
	}
}
