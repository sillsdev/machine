using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIL.Machine.WebApi.Models
{
	public interface IEngineService
	{
		IEnumerable<EngineContext> GetAll();
		bool TryGet(string sourceLanguageTag, string targetLanguageTag, out EngineContext engineContext);
		Task<SessionContext> TryCreateSession(string sourceLanguageTag, string targetLanguageTag);
		Task<string> TryTranslate(string sourceLanguageTag, string targetLanguageTag, string segment);
	}
}
