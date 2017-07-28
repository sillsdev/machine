using System;
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
		private readonly EngineOptions _options;
		private readonly Dictionary<string, Owned<EngineRunner>> _runners;
		private readonly AsyncReaderWriterLock _lock;
		private readonly IEngineRepository _engineRepo;
		private readonly IBuildRepository _buildRepo;
		private readonly Func<string, Owned<EngineRunner>> _engineRunnerFactory;
		private readonly AsyncTimer _commitTimer;

		public EngineService(IOptions<EngineOptions> options, IEngineRepository engineRepo, IBuildRepository buildRepo,
			Func<string, Owned<EngineRunner>> engineRunnerFactory)
		{
			_options = options.Value;
			_engineRepo = engineRepo;
			_buildRepo = buildRepo;
			_engineRunnerFactory = engineRunnerFactory;
			_runners = new Dictionary<string, Owned<EngineRunner>>();
			_lock = new AsyncReaderWriterLock();
			_commitTimer = new AsyncTimer(EngineCommitAsync);
		}

		public void Init()
		{
			// restart any builds that didn't finish before the last shutdown
			foreach (Build build in _buildRepo.GetAll())
			{
				if (_engineRepo.TryGet(build.EngineId, out Engine engine))
				{
					EngineRunner runner = CreateRunner(engine.Id);
					runner.RestartUnfinishedBuild(build, engine);
				}
				else
				{
					// orphaned build, so delete it
					_buildRepo.Delete(build);
				}
			}
			_commitTimer.Start(_options.EngineCommitFrequency);
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

			using (var upgradeKey = await _lock.UpgradeableReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetByLocatorAsync(locatorType, locator);
				if (engine == null)
					return null;
				EngineRunner runner = await GetOrCreateRunnerAsync(upgradeKey, engine.Id);
				return await runner.TranslateAsync(segment.Select(Preprocessors.Lowercase).ToArray());
			}
		}

		public async Task<IEnumerable<TranslationResult>> TranslateAsync(
			EngineLocatorType locatorType, string locator, int n, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (var upgradeKey = await _lock.UpgradeableReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetByLocatorAsync(locatorType, locator);
				if (engine == null)
					return null;
				EngineRunner runner = await GetOrCreateRunnerAsync(upgradeKey, engine.Id);
				return await runner.TranslateAsync(n, segment.Select(Preprocessors.Lowercase).ToArray());
			}
		}

		public async Task<InteractiveTranslationResult> InteractiveTranslateAsync(
			EngineLocatorType locatorType, string locator, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (var upgradeKey = await _lock.UpgradeableReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetByLocatorAsync(locatorType, locator);
				if (engine == null)
					return null;
				EngineRunner runner = await GetOrCreateRunnerAsync(upgradeKey, engine.Id);
				return await runner.InteractiveTranslateAsync(segment.Select(Preprocessors.Lowercase).ToArray());
			}
		}

		public async Task<bool> TrainSegmentAsync(EngineLocatorType locatorType, string locator,
			IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			using (var upgradeKey = await _lock.UpgradeableReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetByLocatorAsync(locatorType, locator);
				if (engine == null)
					return false;
				EngineRunner runner = await GetOrCreateRunnerAsync(upgradeKey, engine.Id);
				await runner.TrainSegmentPairAsync(sourceSegment.Select(Preprocessors.Lowercase).ToArray(),
					targetSegment.Select(Preprocessors.Lowercase).ToArray());
				return true;
			}
		}

		public async Task<Engine> AddProjectAsync(string sourceLanguageTag, string targetLanguageTag,
			string projectId, bool isShared)
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				Engine engine = isShared ? await _engineRepo.GetByLanguageTagAsync(sourceLanguageTag, targetLanguageTag) : null;
				try
				{
					EngineRunner runner;
					if (engine == null)
					{
						// no existing shared engine or a project-specific engine
						engine = new Engine
						{
							Projects = {projectId},
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

				return engine;
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

				if (engine.Projects.Count == 1 && engine.Projects.Contains(projectId))
				{
					// the engine will have no associated projects, so remove it
					await _engineRepo.DeleteAsync(engine);
					if (_runners.TryGetValue(engine.Id, out Owned<EngineRunner> runner))
					{
						_runners.Remove(engine.Id);
						await runner.Value.DeleteDataAsync();
						runner.Dispose();
					}
				}
				else
				{
					// engine will still have associated projects, so just update it
					await _engineRepo.ConcurrentUpdateAsync(engine, e => e.Projects.Remove(projectId));
					await GetOrCreateRunner(engine.Id).StartBuildAsync(engine);
				}
				return true;
			}
		}

		public async Task<Build> StartBuildAsync(EngineLocatorType locatorType, string locator)
		{
			CheckDisposed();

			using (var upgradeKey = await _lock.UpgradeableReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetByLocatorAsync(locatorType, locator);
				if (engine == null)
					return null;
				EngineRunner runner = await GetOrCreateRunnerAsync(upgradeKey, engine.Id);
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

		private async Task<EngineRunner> GetOrCreateRunnerAsync(AsyncReaderWriterLock.UpgradeableReaderKey upgradeKey,
			string engineId)
		{
			if (_runners.TryGetValue(engineId, out Owned<EngineRunner> runner))
				return runner.Value;

			using (await upgradeKey.UpgradeAsync())
				return CreateRunner(engineId);
		}

		private EngineRunner GetOrCreateRunner(string engineId)
		{
			if (_runners.TryGetValue(engineId, out Owned<EngineRunner> runner))
				return runner.Value;

			return CreateRunner(engineId);
		}

		private EngineRunner CreateRunner(string engineId)
		{
			Owned<EngineRunner> runner = _engineRunnerFactory(engineId);
			_runners[engineId] = runner;
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
