using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.OwnedInstances;
using Microsoft.Extensions.Options;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Server.DataAccess;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Options;
using SIL.ObjectModel;
using SIL.Threading;

namespace SIL.Machine.WebApi.Server.Services
{
	public class EngineService : DisposableBase
	{
		private readonly IOptions<EngineOptions> _options;
		private readonly ConcurrentDictionary<string, Owned<EngineRunner>> _runners;
		private readonly AsyncReaderWriterLock _lock;
		private readonly IEngineRepository _engineRepo;
		private readonly IBuildRepository _buildRepo;
		private readonly IRepository<Project> _projectRepo;
		private readonly Func<string, Owned<EngineRunner>> _engineRunnerFactory;
		private readonly AsyncTimer _commitTimer;

		public EngineService(IOptions<EngineOptions> options, IEngineRepository engineRepo, IBuildRepository buildRepo,
			IRepository<Project> projectRepo, Func<string, Owned<EngineRunner>> engineRunnerFactory)
		{
			_options = options;
			_engineRepo = engineRepo;
			_buildRepo = buildRepo;
			_projectRepo = projectRepo;
			_engineRunnerFactory = engineRunnerFactory;
			_runners = new ConcurrentDictionary<string, Owned<EngineRunner>>();
			_lock = new AsyncReaderWriterLock();
			_commitTimer = new AsyncTimer(EngineCommitAsync);
		}

		public async Task InitAsync()
		{
			// restart any builds that didn't finish before the last shutdown
			foreach (Build build in await _buildRepo.GetAllAsync())
			{
				Engine engine = await _engineRepo.GetAsync(build.EngineId);
				if (engine != null)
				{
					EngineRunner runner = CreateRunner(engine.Id);
					await runner.RestartUnfinishedBuildAsync(build, engine);
				}
				else
				{
					// orphaned build, so delete it
					await _buildRepo.DeleteAsync(build);
				}
			}
			_commitTimer.Start(_options.Value.EngineCommitFrequency);
		}

		private async Task EngineCommitAsync()
		{
			using (await _lock.ReaderLockAsync())
			{
				foreach (Owned<EngineRunner> runner in _runners.Values)
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
				EngineRunner runner = GetOrCreateRunner(engine.Id);
				return await runner.TranslateAsync(segment.Select(Preprocessors.Lowercase).ToArray());
			}
		}

		public async Task<IEnumerable<TranslationResult>> TranslateAsync(
			EngineLocatorType locatorType, string locator, int n, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetByLocatorAsync(locatorType, locator);
				if (engine == null)
					return null;
				EngineRunner runner = GetOrCreateRunner(engine.Id);
				return await runner.TranslateAsync(n, segment.Select(Preprocessors.Lowercase).ToArray());
			}
		}

		public async Task<InteractiveTranslationResult> InteractiveTranslateAsync(
			EngineLocatorType locatorType, string locator, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetByLocatorAsync(locatorType, locator);
				if (engine == null)
					return null;
				EngineRunner runner = GetOrCreateRunner(engine.Id);
				return await runner.InteractiveTranslateAsync(segment.Select(Preprocessors.Lowercase).ToArray());
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
				EngineRunner runner = GetOrCreateRunner(engine.Id);
				await runner.TrainSegmentPairAsync(sourceSegment.Select(Preprocessors.Lowercase).ToArray(),
					targetSegment.Select(Preprocessors.Lowercase).ToArray());
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
					EngineRunner runner;
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
						runner = CreateRunner(engine.Id);
						await runner.InitNewAsync();
					}
					else
					{
						// found an existing shared engine
						if (engine.Projects.Contains(projectId))
							return null;
						engine = await _engineRepo.ConcurrentUpdateAsync(engine, e => e.Projects.Add(projectId));
						runner = GetOrCreateRunner(engine.Id);
					}
					await runner.StartBuildAsync(engine);
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
					Engine = engine.Id
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

				EngineRunner runner = GetOrCreateRunner(engine.Id);
				if (engine.Projects.Count == 1 && engine.Projects.Contains(projectId))
				{
					// the engine will have no associated projects, so remove it
					await _engineRepo.DeleteAsync(engine);
					_runners.TryRemove(engine.Id, out _);
					await runner.DeleteDataAsync();
					runner.Dispose();
				}
				else
				{
					// engine will still have associated projects, so just update it
					await _engineRepo.ConcurrentUpdateAsync(engine, e => e.Projects.Remove(projectId));
					await runner.StartBuildAsync(engine);
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
				EngineRunner runner = GetOrCreateRunner(engine.Id);
				return await runner.StartBuildAsync(engine);
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
				if (_runners.TryGetValue(build.EngineId, out Owned<EngineRunner> runner))
					await runner.Value.CancelBuildAsync();
				return true;
			}
		}

		private EngineRunner GetOrCreateRunner(string engineId)
		{
			return _runners.GetOrAdd(engineId, _engineRunnerFactory).Value;
		}

		private EngineRunner CreateRunner(string engineId)
		{
			Owned<EngineRunner> runner = _engineRunnerFactory(engineId);
			_runners.TryAdd(engineId, runner);
			return runner.Value;
		}

		protected override void DisposeManagedResources()
		{
			_commitTimer.Dispose();
			foreach (Owned<EngineRunner> runner in _runners.Values)
				runner.Dispose();
			_runners.Clear();
		}
	}
}
