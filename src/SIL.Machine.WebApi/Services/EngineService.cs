using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac.Features.OwnedInstances;
using Microsoft.Extensions.Options;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Configuration;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Services
{
	public class EngineService : DisposableBase
	{
		private readonly IOptions<EngineOptions> _engineOptions;
		private readonly ConcurrentDictionary<string, Owned<EngineRuntime>> _runtimes;
		private readonly AsyncReaderWriterLock _lock;
		private readonly IEngineRepository _engineRepo;
		private readonly IBuildRepository _buildRepo;
		private readonly IRepository<Project> _projectRepo;
		private readonly Func<string, Owned<EngineRuntime>> _engineRunnerFactory;
		private readonly AsyncTimer _commitTimer;

		public EngineService(IOptions<EngineOptions> engineOptions, IEngineRepository engineRepo,
			IBuildRepository buildRepo, IRepository<Project> projectRepo,
			Func<string, Owned<EngineRuntime>> engineRuntimeFactory)
		{
			_engineOptions = engineOptions;
			_engineRepo = engineRepo;
			_buildRepo = buildRepo;
			_projectRepo = projectRepo;
			_engineRunnerFactory = engineRuntimeFactory;
			_runtimes = new ConcurrentDictionary<string, Owned<EngineRuntime>>();
			_lock = new AsyncReaderWriterLock();
			_commitTimer = new AsyncTimer(EngineCommitAsync);
		}

		public void Init()
		{
			_commitTimer.Start(_engineOptions.Value.EngineCommitFrequency);
		}

		private async Task EngineCommitAsync()
		{
			using (await _lock.ReaderLockAsync())
			{
				foreach (Owned<EngineRuntime> runner in _runtimes.Values)
					await runner.Value.CommitAsync();
			}
		}

		public async Task<TranslationResult> TranslateAsync(EngineLocatorType locatorType,
			string locator, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetByLocatorAsync(locatorType, locator);
				if (engine == null)
					return null;
				EngineRuntime runtime = GetOrCreateRuntime(engine.Id);
				return await runtime.TranslateAsync(segment);
			}
		}

		public async Task<IEnumerable<TranslationResult>> TranslateAsync(EngineLocatorType locatorType, string locator,
			int n, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetByLocatorAsync(locatorType, locator);
				if (engine == null)
					return null;
				EngineRuntime runtime = GetOrCreateRuntime(engine.Id);
				return await runtime.TranslateAsync(n, segment);
			}
		}

		public async Task<HybridInteractiveTranslationResult> InteractiveTranslateAsync(EngineLocatorType locatorType,
			string locator, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetByLocatorAsync(locatorType, locator);
				if (engine == null)
					return null;
				EngineRuntime runtime = GetOrCreateRuntime(engine.Id);
				return await runtime.InteractiveTranslateAsync(segment);
			}
		}

		public async Task<bool> TrainSegmentAsync(EngineLocatorType locatorType, string locator,
			IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetByLocatorAsync(locatorType, locator);
				if (engine == null)
					return false;
				EngineRuntime runtime = GetOrCreateRuntime(engine.Id);
				await runtime.TrainSegmentPairAsync(sourceSegment, targetSegment);
				return true;
			}
		}

		public async Task<Project> AddProjectAsync(string projectId, string sourceLanguageTag, string targetLanguageTag,
			string sourceSegmentType, string targetSegmentType, bool isShared)
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				Engine engine = isShared
					? await _engineRepo.GetByLanguageTagAsync(sourceLanguageTag, targetLanguageTag)
					: null;
				try
				{
					if (engine == null)
					{
						// no existing shared engine or a project-specific engine
						engine = new Engine
						{
							Projects = { projectId },
							IsShared = isShared,
							SourceLanguageTag = sourceLanguageTag,
							TargetLanguageTag = targetLanguageTag
						};
						await _engineRepo.InsertAsync(engine);
						EngineRuntime runtime = CreateRuntime(engine.Id);
						await runtime.InitNewAsync();
					}
					else
					{
						// found an existing shared engine
						if (engine.Projects.Contains(projectId))
							return null;
						engine = await _engineRepo.ConcurrentUpdateAsync(engine, e => e.Projects.Add(projectId));
					}
				}
				catch (KeyAlreadyExistsException)
				{
					// a project with the same id already exists
					return null;
				}

				var project = new Project
				{
					Id = projectId,
					SourceLanguageTag = sourceLanguageTag,
					TargetLanguageTag = targetLanguageTag,
					SourceSegmentType = sourceSegmentType,
					TargetSegmentType = targetSegmentType,
					IsShared = isShared,
					EngineRef = engine.Id
				};
				await _projectRepo.InsertAsync(project);
				return project;
			}
		}

		public async Task<bool> RemoveProjectAsync(string projectId)
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				Engine engine = await _engineRepo.GetByProjectIdAsync(projectId);
				if (engine == null)
					return false;

				EngineRuntime runtime = GetOrCreateRuntime(engine.Id);
				if (engine.Projects.Count == 1 && engine.Projects.Contains(projectId))
				{
					// the engine will have no associated projects, so remove it
					await _engineRepo.DeleteAsync(engine);
					_runtimes.TryRemove(engine.Id, out _);
					await runtime.DeleteDataAsync();
					runtime.Dispose();
				}
				else
				{
					// engine will still have associated projects, so just update it
					await _engineRepo.ConcurrentUpdateAsync(engine, e => e.Projects.Remove(projectId));
				}
				await _projectRepo.DeleteAsync(projectId);
				return true;
			}
		}

		public async Task<Build> StartBuildAsync(EngineLocatorType locatorType, string locator)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetByLocatorAsync(locatorType, locator);
				if (engine == null)
					return null;
				EngineRuntime runtime = GetOrCreateRuntime(engine.Id);
				return await runtime.StartBuildAsync();
			}
		}

		public async Task<bool> CancelBuildAsync(BuildLocatorType locatorType, string locator)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				Build build = await _buildRepo.GetByLocatorAsync(locatorType, locator);
				if (build == null)
					return false;
				if (TryGetRuntime(build.EngineRef, out EngineRuntime runtime))
					await runtime.CancelBuildAsync();
				return true;
			}
		}

		public async Task<(Engine Engine, EngineRuntime Runtime)> GetEngineAsync(string engineId)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetAsync(engineId);
				if (engine == null)
					return (null, null);
				return (engine, GetOrCreateRuntime(engineId));
			}
		}

		internal EngineRuntime GetOrCreateRuntime(string engineId)
		{
			return _runtimes.GetOrAdd(engineId, _engineRunnerFactory).Value;
		}

		private EngineRuntime CreateRuntime(string engineId)
		{
			Owned<EngineRuntime> runtime = _engineRunnerFactory(engineId);
			_runtimes.TryAdd(engineId, runtime);
			return runtime.Value;
		}

		private bool TryGetRuntime(string engineId, out EngineRuntime runtime)
		{
			if (_runtimes.TryGetValue(engineId, out Owned<EngineRuntime> ownedRuntime))
			{
				runtime = ownedRuntime.Value;
				return true;
			}

			runtime = null;
			return false;
		}

		protected override void DisposeManagedResources()
		{
			_commitTimer.Dispose();
			foreach (Owned<EngineRuntime> runtime in _runtimes.Values)
				runtime.Dispose();
			_runtimes.Clear();
		}
	}
}
