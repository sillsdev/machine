using System.Collections.Generic;

namespace SIL.Machine.WebApi.Models
{
	public interface IEngineService
	{
		IEnumerable<EngineDto> GetAll();
		bool TryGet(string sourceLanguageTag, string targetLanguageTag, out EngineDto engine);
		bool TryCreateSession(string sourceLanguageTag, string targetLanguageTag, out SessionDto session);
		bool TryTranslate(string sourceLanguageTag, string targetLanguageTag, string segment, out string result);
	}
}
