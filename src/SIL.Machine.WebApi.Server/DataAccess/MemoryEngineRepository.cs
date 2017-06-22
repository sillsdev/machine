using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Server.Models;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public class MemoryEngineRepository : MemoryRepositoryBase<Engine>, IEngineRepository
	{
		private readonly UniqueEntityIndex<(string SourceLanguageTag, string TargetLanguageTag), Engine> _langTagIndex;
		private readonly UniqueEntityIndex<string, Engine> _projectIndex;

		public MemoryEngineRepository(IEngineRepository persistenceRepo = null)
			: base(persistenceRepo)
		{
			_langTagIndex = new UniqueEntityIndex<(string SourceLanguageTag, string TargetLanguageTag), Engine>(
				e => (e.SourceLanguageTag, e.TargetLanguageTag), e => e.IsShared);
			if (PersistenceRepository != null)
				_langTagIndex.PopulateIndex(PersistenceRepository.GetAll());

			_projectIndex = new UniqueEntityIndex<string, Engine>(e => e.Projects);
			if (PersistenceRepository != null)
				_projectIndex.PopulateIndex(PersistenceRepository.GetAll());
		}

		public async Task<Engine> GetByLanguageTagAsync(string sourceLanguageTag, string targetLanguageTag)
		{
			using (await Lock.ReaderLockAsync())
			{
				if (_langTagIndex.TryGetEntity((sourceLanguageTag, targetLanguageTag), out Engine engine))
					return engine;
				return null;
			}
		}

		public async Task<Engine> GetByProjectIdAsync(string projectId)
		{
			using (await Lock.ReaderLockAsync())
			{
				if (_projectIndex.TryGetEntity(projectId, out Engine engine))
					return engine;
				return null;
			}
		}

		protected override void OnBeforeEntityInserted(Engine engine)
		{
			_langTagIndex.CheckKeyConflict(engine);
			_projectIndex.CheckKeyConflict(engine);
		}

		protected override void OnBeforeEntityUpdated(Engine engine)
		{
			_langTagIndex.CheckKeyConflict(engine);
			_projectIndex.CheckKeyConflict(engine);
		}

		protected override void OnEntityInserted(Engine internalEngine, IList<Action<EntityChange<Engine>>> changeListeners)
		{
			_langTagIndex.OnEntityInserted(internalEngine, changeListeners);
			_projectIndex.OnEntityInserted(internalEngine, changeListeners);
		}

		protected override void OnEntityUpdated(Engine internalEngine, IList<Action<EntityChange<Engine>>> changeListeners)
		{
			_langTagIndex.OnEntityUpdated(internalEngine, changeListeners);
			_projectIndex.OnEntityUpdated(internalEngine, changeListeners);
		}

		protected override void OnEntityDeleted(Engine internalEngine, IList<Action<EntityChange<Engine>>> changeListeners)
		{
			_langTagIndex.OnEntityDeleted(internalEngine, changeListeners);
			_projectIndex.OnEntityDeleted(internalEngine, changeListeners);
		}
	}
}
