using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Threading;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.DataAccess.Memory
{
	public class MemoryEngineRepository : MemoryRepository<Engine>, IEngineRepository
	{
		private readonly UniqueEntityIndex<(string SourceLanguageTag, string TargetLanguageTag), Engine> _langTagIndex;
		private readonly UniqueEntityIndex<string, Engine> _projectIndex;

		public MemoryEngineRepository(IEngineRepository persistenceRepo = null)
			: base(persistenceRepo)
		{
			_langTagIndex = new UniqueEntityIndex<(string SourceLanguageTag, string TargetLanguageTag), Engine>(
				Lock, e => (e.SourceLanguageTag, e.TargetLanguageTag), e => e.IsShared);
			_projectIndex = new UniqueEntityIndex<string, Engine>(Lock, e => e.Projects);
		}

		public override void Init()
		{
			base.Init();
			if (PersistenceRepository != null)
			{
				Engine[] engines = PersistenceRepository.GetAllAsync().WaitAndUnwrapException().ToArray();
				_langTagIndex.PopulateIndex(engines);
				_projectIndex.PopulateIndex(engines);
			}
		}

		public async Task<Engine> GetByLanguageTagAsync(string sourceLanguageTag, string targetLanguageTag,
			CancellationToken ct = default(CancellationToken))
		{
			using (await Lock.ReaderLockAsync(ct))
			{
				if (_langTagIndex.TryGetEntity((sourceLanguageTag, targetLanguageTag), out Engine engine))
					return engine;
				return null;
			}
		}

		public async Task<Engine> GetByProjectIdAsync(string projectId,
			CancellationToken ct = default(CancellationToken))
		{
			using (await Lock.ReaderLockAsync(ct))
			{
				if (_projectIndex.TryGetEntity(projectId, out Engine engine))
					return engine;
				return null;
			}
		}

		protected override void OnBeforeEntityChanged(EntityChangeType type, Engine engine)
		{
			_langTagIndex.CheckKeyConflict(engine);
			_projectIndex.CheckKeyConflict(engine);
		}

		protected override void OnEntityChanged(EntityChangeType type, Engine oldEngine, Engine newEngine,
			IList<Subscription<Engine>> allSubscriptions)
		{
			switch (type)
			{
				case EntityChangeType.Insert:
				case EntityChangeType.Update:
					_langTagIndex.OnEntityUpdated(oldEngine, newEngine, allSubscriptions);
					_projectIndex.OnEntityUpdated(oldEngine, newEngine, allSubscriptions);
					break;
				case EntityChangeType.Delete:
					_langTagIndex.OnEntityDeleted(oldEngine, allSubscriptions);
					_projectIndex.OnEntityDeleted(oldEngine, allSubscriptions);
					break;
			}
		}
	}
}
