using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using SIL.Extensions;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.Mongo
{
	public class MongoBuildRepository : MongoRepository<Build>, IBuildRepository
	{
		private readonly Dictionary<string, ISet<Subscription<Build>>> _engineIdSubscriptions;

		public MongoBuildRepository(IMongoCollection<Build> collection)
			: base(collection)
		{
			_engineIdSubscriptions = new Dictionary<string, ISet<Subscription<Build>>>();
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
