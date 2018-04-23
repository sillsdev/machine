using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NoDb;
using SIL.Machine.WebApi.Server.Models;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public class NoDbBuildRepository : NoDbRepository<Build>, IBuildRepository
	{
		public NoDbBuildRepository(IBasicCommands<Build> commands, IBasicQueries<Build> queries)
			: base(commands, queries)
		{
		}

		public async Task<Build> GetByEngineIdAsync(string engineId)
		{
			IEnumerable<Build> builds = await Queries.GetAllAsync(NoDbProjectId);
			return builds.SingleOrDefault(b => b.EngineId == engineId);
		}

		public Task<IDisposable> SubscribeByEngineIdAsync(string engineId, Action<EntityChange<Build>> listener)
		{
			throw new NotSupportedException();
		}
	}
}
