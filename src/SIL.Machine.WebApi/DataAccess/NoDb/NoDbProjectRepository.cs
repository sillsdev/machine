using NoDb;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.NoDb
{
	public class NoDbProjectRepository : NoDbRepository<Project>, IProjectRepository
	{
		public NoDbProjectRepository(IBasicCommands<Project> commands, IBasicQueries<Project> queries)
			: base(commands, queries)
		{
		}
	}
}
