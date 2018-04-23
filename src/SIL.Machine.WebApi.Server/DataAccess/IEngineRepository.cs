using System.Threading.Tasks;
using SIL.Machine.WebApi.Server.Models;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public interface IEngineRepository : IRepository<Engine>
	{
		Task<Engine> GetByLanguageTagAsync(string sourceLanguageTag, string targetLanguageTag);
		Task<Engine> GetByProjectIdAsync(string projectId);
	}
}
