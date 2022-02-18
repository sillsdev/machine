using NoDb;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.NoDb
{
	public class NoDbEngineRepository : NoDbRepository<Engine>, IEngineRepository
	{
		public NoDbEngineRepository(IBasicCommands<Engine> commands, IBasicQueries<Engine> queries)
			: base(commands, queries)
		{
		}
	}
}
