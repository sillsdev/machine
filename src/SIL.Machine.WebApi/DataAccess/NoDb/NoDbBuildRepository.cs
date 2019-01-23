using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NoDb;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.NoDb
{
	public class NoDbBuildRepository : NoDbRepository<Build>, IBuildRepository
	{
		public NoDbBuildRepository(IBasicCommands<Build> commands, IBasicQueries<Build> queries)
			: base(commands, queries)
		{
		}

		public async Task<Build> GetByEngineIdAsync(string engineId, CancellationToken ct = default(CancellationToken))
		{
			IEnumerable<Build> builds = await Queries.GetAllAsync(NoDbProjectId, ct);
			return builds.SingleOrDefault(b => b.EngineRef == engineId);
		}

		public Task<Subscription<Build>> SubscribeByEngineIdAsync(string engineId,
			CancellationToken ct = default(CancellationToken))
		{
			throw new NotSupportedException();
		}
	}
}
