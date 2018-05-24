using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using SIL.Extensions;
using SIL.Machine.WebApi.Server.Models;

namespace SIL.Machine.WebApi.Server.DataAccess.Mongo
{
	public class MongoBuildRepository : MongoRepository<Build>, IBuildRepository
	{
		private readonly Dictionary<string, ISet<Action<EntityChange<Build>>>> _engineIdChangeListeners;

		public MongoBuildRepository(IMongoCollection<Build> collection)
			: base(collection)
		{
			_engineIdChangeListeners = new Dictionary<string, ISet<Action<EntityChange<Build>>>>();
		}

		public Task<Build> GetByEngineIdAsync(string engineId)
		{
			return Collection.Find(b => b.EngineRef == engineId && b.State == BuildStates.Active).FirstOrDefaultAsync();
		}

		public async Task<IDisposable> SubscribeByEngineIdAsync(string engineId, Action<EntityChange<Build>> listener)
		{
			using (await Lock.LockAsync())
				return new MongoSubscription<string, Build>(Lock, _engineIdChangeListeners, engineId, listener);
		}

		protected override void GetChangeListeners(Build build, IList<Action<EntityChange<Build>>> changeListeners)
		{
			base.GetChangeListeners(build, changeListeners);
			if (_engineIdChangeListeners.TryGetValue(build.EngineRef, out ISet<Action<EntityChange<Build>>> listeners))
				changeListeners.AddRange(listeners);
		}
	}
}
