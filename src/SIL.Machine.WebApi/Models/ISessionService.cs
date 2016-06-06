namespace SIL.Machine.WebApi.Models
{
	public interface ISessionService
	{
		void Add(SessionContext sessionContext);
		bool TryGet(string id, out SessionContext sessionContext);
		bool Remove(string id);
		bool TryStartTranslation(string id, string segment, out Suggestion suggestion);
		bool TryUpdatePrefix(string id, string prefix, out Suggestion suggestion);
		bool TryApprove(string id);
	}
}
