using System.Threading.Tasks;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess
{
	public interface IBuildRepository : IRepository<Build>
	{
		Task<Build> GetByEngineIdAsync(string engineId);
		Task<Build> GetByProjectIdAsync(string projectId);
	}
}
