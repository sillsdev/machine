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
	internal class EngineService : DisposableBase, IEngineServiceInternal
	{
		private readonly IOptions<EngineOptions> _engineOptions;
		private readonly ConcurrentDictionary<string, Owned<EngineRuntime>> _runtimes;
		private readonly AsyncReaderWriterLock _lock;
		private readonly IEngineRepository _engines;
		private readonly IBuildRepository _builds;
		private readonly IProjectRepository _projects;
		private readonly Func<string, Owned<EngineRuntime>> _engineRunnerFactory;
		private readonly AsyncTimer _commitTimer;

		public EngineService(IOptions<EngineOptions> engineOptions, IEngineRepository engines,
			IBuildRepository builds, IProjectRepository projects,
			Func<string, Owned<EngineRuntime>> engineRuntimeFactory)
		{
			_engineOptions = engineOptions;
			_engines = engines;
			_builds = builds;
			_projects = projects;
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

		public async Task<TranslationResult> TranslateAsync(string engineId, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				if (!await _engines.ExistsAsync(engineId))
					return null;
				EngineRuntime runtime = GetOrCreateRuntime(engineId);
				return await runtime.TranslateAsync(segment);
			}
		}

		public async Task<IEnumerable<TranslationResult>> TranslateAsync(string engineId, int n,
			IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				if (!await _engines.ExistsAsync(engineId))
					return null;
				EngineRuntime runtime = GetOrCreateRuntime(engineId);
				return await runtime.TranslateAsync(n, segment);
			}
		}

		public async Task<HybridInteractiveTranslationResult> InteractiveTranslateAsync(string engineId,
			IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				if (!await _engines.ExistsAsync(engineId))
					return null;
				EngineRuntime runtime = GetOrCreateRuntime(engineId);
				return await runtime.InteractiveTranslateAsync(segment);
			}
		}

		public async Task<bool> TrainSegmentAsync(string engineId, IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				if (!await _engines.ExistsAsync(engineId))
					return false;
				EngineRuntime runtime = GetOrCreateRuntime(engineId);
				await runtime.TrainSegmentPairAsync(sourceSegment, targetSegment);
				return true;
			}
		}

		public async Task<bool> AddProjectAsync(Project project)
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				Engine engine = project.IsShared
					? await _engines.GetByLanguageTagAsync(project.SourceLanguageTag, project.TargetLanguageTag)
					: null;
				try
				{
					if (engine == null)
					{
						// no existing shared engine or a project-specific engine
						engine = new Engine
						{
							Projects = { project.Id },
							IsShared = project.IsShared,
							SourceLanguageTag = project.SourceLanguageTag,
							TargetLanguageTag = project.TargetLanguageTag
						};
						await _engines.InsertAsync(engine);
						EngineRuntime runtime = CreateRuntime(engine.Id);
						await runtime.InitNewAsync();
					}
					else
					{
						// found an existing shared engine
						if (engine.Projects.Contains(project.Id))
							return false;
						engine = await _engines.ConcurrentUpdateAsync(engine, e => e.Projects.Add(project.Id));
					}
				}
				catch (KeyAlreadyExistsException)
				{
					// a project with the same id already exists
					return false;
				}

				project.EngineRef = engine.Id;
				await _projects.InsertAsync(project);
				return true;
			}
		}

		public async Task<bool> RemoveProjectAsync(string projectId)
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				Engine engine = await _engines.GetByProjectIdAsync(projectId);
				if (engine == null)
					return false;

				EngineRuntime runtime = GetOrCreateRuntime(engine.Id);
				if (engine.Projects.Count == 1 && engine.Projects.Contains(projectId))
				{
					// the engine will have no associated projects, so remove it
					_runtimes.TryRemove(engine.Id, out _);
					await runtime.DeleteDataAsync();
					runtime.Dispose();
					await _engines.DeleteAsync(engine);
					await _builds.DeleteAllByEngineIdAsync(engine.Id);
				}
				else
				{
					// engine will still have associated projects, so just update it
					await _engines.ConcurrentUpdateAsync(engine, e => e.Projects.Remove(projectId));
				}
				await _projects.DeleteAsync(projectId);
				return true;
			}
		}

		public async Task<Build> StartBuildAsync(string engineId)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				if (!await _engines.ExistsAsync(engineId))
					return null;
				EngineRuntime runtime = GetOrCreateRuntime(engineId);
				return await runtime.StartBuildAsync();
			}
		}

		public async Task<Build> StartBuildByProjectIdAsync(string projectId)
		{
			CheckDisposed();
			Engine engine = await _engines.GetByProjectIdAsync(projectId);
			return await StartBuildAsync(engine.Id);
		}

		public async Task CancelBuildAsync(string engineId)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				if (TryGetRuntime(engineId, out EngineRuntime runtime))
					await runtime.CancelBuildAsync();
			}
		}

		public async Task<(Engine Engine, EngineRuntime Runtime)> GetEngineAsync(string engineId)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				Engine engine = await _engines.GetAsync(engineId);
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
