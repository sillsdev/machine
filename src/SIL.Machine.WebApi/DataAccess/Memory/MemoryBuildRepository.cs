using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Threading;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.Memory
{
	public class MemoryBuildRepository : MemoryRepository<Build>, IBuildRepository
	{
		private readonly UniqueEntityIndex<string, Build> _engineIdIndex;

		public MemoryBuildRepository(IBuildRepository persistenceRepo = null)
			: base(persistenceRepo)
		{
			_engineIdIndex = new UniqueEntityIndex<string, Build>(Lock, b => b.EngineRef,
				b => b.State == BuildStates.Active || b.State == BuildStates.Pending);
		}

		public override void Init()
		{
			base.Init();
			if (PersistenceRepository != null)
				_engineIdIndex.PopulateIndex(PersistenceRepository.GetAllAsync().WaitAndUnwrapException());
		}

		public async Task<Build> GetByEngineIdAsync(string engineId, CancellationToken ct = default(CancellationToken))
		{
			using (await Lock.ReaderLockAsync(ct))
			{
				if (_engineIdIndex.TryGetEntity(engineId, out Build build))
					return build;
				return null;
			}
		}

		public Task<Subscription<Build>> SubscribeByEngineIdAsync(string engineId,
			CancellationToken ct = default(CancellationToken))
		{
			return _engineIdIndex.SubscribeAsync(engineId, ct);
		}

		public async Task DeleteAllByEngineIdAsync(string engineId, CancellationToken ct = default(CancellationToken))
		{
			var deletedBuilds = new List<(Build, List<Subscription<Build>>)>();
			using (await Lock.WriterLockAsync(ct))
			{
				foreach (string buildId in Entities.Values.Where(e => e.EngineRef == engineId).Select(e => e.Id)
					.ToArray())
				{
					var allSubscriptions = new List<Subscription<Build>>();
					Build internalBuild = DeleteEntity(buildId, allSubscriptions);

					if (PersistenceRepository != null)
						await PersistenceRepository.DeleteAsync(buildId, ct);
					deletedBuilds.Add((internalBuild, allSubscriptions));
				}
			}
			foreach ((Build internalBuild, List<Subscription<Build>> allSubscriptions) in deletedBuilds)
				SendToSubscribers(allSubscriptions, EntityChangeType.Delete, internalBuild);
		}

		protected override void OnBeforeEntityChanged(EntityChangeType type, Build build)
		{
			_engineIdIndex.CheckKeyConflict(build);
		}

		protected override void OnEntityChanged(EntityChangeType type, Build oldBuild, Build newBulid,
			IList<Subscription<Build>> allSubscriptions)
		{
			switch (type)
			{
				case EntityChangeType.Insert:
				case EntityChangeType.Update:
					_engineIdIndex.OnEntityUpdated(oldBuild, newBulid, allSubscriptions);
					break;
				case EntityChangeType.Delete:
					_engineIdIndex.OnEntityDeleted(oldBuild, allSubscriptions);
					break;
			}
			
		}
	}
}
