namespace SIL.Machine.WebApi.Services;

public interface IBuildService
{
	Task<IEnumerable<Build>> GetAllAsync(string parentId);
	Task<Build?> GetAsync(string id, CancellationToken cancellationToken = default);
	Task<Build?> GetActiveAsync(string parentId, CancellationToken cancellationToken = default);
	Task<EntityChange<Build>> GetNewerRevisionAsync(string id, long minRevision,
		CancellationToken cancellationToken = default);
	Task<EntityChange<Build>> GetActiveNewerRevisionAsync(string parentId, long minRevision,
		CancellationToken cancellationToken = default);
}
