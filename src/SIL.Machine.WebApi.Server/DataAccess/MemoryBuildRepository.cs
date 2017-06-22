using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Server.Models;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public class MemoryBuildRepository : MemoryRepositoryBase<Build>, IBuildRepository
	{
		private readonly UniqueEntityIndex<string, Build> _engineIdIndex;

		public MemoryBuildRepository(IBuildRepository persistenceRepo = null)
			: base(persistenceRepo)
		{
			_engineIdIndex = new UniqueEntityIndex<string, Build>(b => b.EngineId);
			if (PersistenceRepository != null)
				_engineIdIndex.PopulateIndex(PersistenceRepository.GetAll());
		}

		public async Task<Build> GetByEngineIdAsync(string engineId)
		{
			using (await Lock.ReaderLockAsync())
			{
				if (_engineIdIndex.TryGetEntity(engineId, out Build build))
					return build;
				return null;
			}
		}

		public async Task<IDisposable> SubscribeByEngineIdAsync(string engineId, Action<EntityChange<Build>> listener)
		{
			using (await Lock.WriterLockAsync())
				return _engineIdIndex.Subscribe(Lock, engineId, listener);
		}

		protected override void OnBeforeEntityInserted(Build build)
		{
			_engineIdIndex.CheckKeyConflict(build);
		}

		protected override void OnBeforeEntityUpdated(Build build)
		{
			_engineIdIndex.CheckKeyConflict(build);
		}

		protected override void OnEntityInserted(Build internalBuild, IList<Action<EntityChange<Build>>> changeListeners)
		{
			_engineIdIndex.OnEntityInserted(internalBuild, changeListeners);
		}

		protected override void OnEntityUpdated(Build internalBuild, IList<Action<EntityChange<Build>>> changeListeners)
		{
			_engineIdIndex.OnEntityUpdated(internalBuild, changeListeners);
		}

		protected override void OnEntityDeleted(Build internalBuild, IList<Action<EntityChange<Build>>> changeListeners)
		{
			_engineIdIndex.OnEntityDeleted(internalBuild, changeListeners);
		}
	}
}
