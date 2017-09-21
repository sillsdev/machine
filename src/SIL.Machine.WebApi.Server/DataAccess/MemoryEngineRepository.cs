using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIL.Machine.WebApi.Server.Models;

namespace SIL.Machine.WebApi.Server.DataAccess
{
	public class MemoryEngineRepository : MemoryRepository<Engine>, IEngineRepository
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

		protected override void OnBeforeEntityChanged(EntityChangeType type, Engine engine)
		{
			_langTagIndex.CheckKeyConflict(engine);
			_projectIndex.CheckKeyConflict(engine);
		}

		protected override void OnEntityChanged(EntityChangeType type, Engine oldEngine, Engine newEngine,
			IList<Action<EntityChange<Engine>>> changeListeners)
		{
			switch (type)
			{
				case EntityChangeType.Insert:
				case EntityChangeType.Update:
					_langTagIndex.OnEntityUpdated(oldEngine, newEngine, changeListeners);
					_projectIndex.OnEntityUpdated(oldEngine, newEngine, changeListeners);
					break;
				case EntityChangeType.Delete:
					_langTagIndex.OnEntityDeleted(oldEngine, changeListeners);
					_projectIndex.OnEntityDeleted(oldEngine, changeListeners);
					break;
			}
		}
	}
}
