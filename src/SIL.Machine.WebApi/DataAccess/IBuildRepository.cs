namespace SIL.Machine.WebApi.DataAccess;

public interface IBuildRepository : IRepository<Build>
{
	Task<Build> GetByEngineIdAsync(string engineId, CancellationToken ct = default);

	Task<Subscription<Build>> SubscribeByEngineIdAsync(string engineId, CancellationToken ct = default);

	Task DeleteAllByEngineIdAsync(string engineId, CancellationToken ct = default);
}
