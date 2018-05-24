using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Server.Models;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public interface IEngineRepository : IRepository<Engine>
	{
		Task<Engine> GetByLanguageTagAsync(string sourceLanguageTag, string targetLanguageTag,
			CancellationToken ct = default(CancellationToken));
		Task<Engine> GetByProjectIdAsync(string projectId, CancellationToken ct = default(CancellationToken));
	}
}
