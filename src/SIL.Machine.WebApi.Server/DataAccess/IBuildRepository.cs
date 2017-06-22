using System.Threading.Tasks;
using SIL.Machine.WebApi.Server.Models;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public interface IBuildRepository : IRepository<Build>
	{
		Task<Build> GetByEngineIdAsync(string engineId);
	}
}
