using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SIL.Extensions;
using SIL.Machine.WebApi.Configuration;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.Mongo
{
	public class MongoBuildRepository : MongoRepository<Build>, IBuildRepository
	{
		private readonly Dictionary<string, ISet<Subscription<Build>>> _engineIdSubscriptions;

		public MongoBuildRepository(IOptions<MongoDataAccessOptions> options)
			: base(options, "builds")
		{
			_engineIdSubscriptions = new Dictionary<string, ISet<Subscription<Build>>>();
		}

		public override void Init()
		{
			CreateOrUpdateIndex(new CreateIndexModel<Build>(Builders<Build>.IndexKeys
				.Ascending(b => b.EngineRef)));
		}

		public Task<Build> GetByEngineIdAsync(string engineId, CancellationToken ct = default(CancellationToken))
		{
			return Collection.Find(b => b.EngineRef == engineId
				&& (b.State == BuildStates.Active || b.State == BuildStates.Pending)).FirstOrDefaultAsync(ct);
		}

		public Task<Subscription<Build>> SubscribeByEngineIdAsync(string engineId,
			CancellationToken ct = default(CancellationToken))
		{
			return AddSubscriptionAsync(GetByEngineIdAsync, _engineIdSubscriptions, engineId, ct);
		}

		protected override void GetSubscriptions(Build build, IList<Subscription<Build>> allSubscriptions)
		{
			base.GetSubscriptions(build, allSubscriptions);
			if (_engineIdSubscriptions.TryGetValue(build.EngineRef, out ISet<Subscription<Build>> subscriptions))
				allSubscriptions.AddRange(subscriptions);
		}
	}
}
