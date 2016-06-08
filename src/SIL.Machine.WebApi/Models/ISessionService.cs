namespace SIL.Machine.WebApi.Models
{
	public interface ISessionService
	{
		void Add(SessionContext sessionContext);
		bool TryGet(string id, out SessionDto session);
		bool Remove(string id);
		bool TryStartTranslation(string id, string segment, out SuggestionDto suggestion);
		bool TryUpdatePrefix(string id, string prefix, out SuggestionDto suggestion);
		bool TryApprove(string id);
	}
}
