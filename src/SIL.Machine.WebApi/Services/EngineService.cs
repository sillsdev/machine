using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Options;
using SIL.ObjectModel;
using SIL.Threading;

namespace SIL.Machine.WebApi.Services
{
	public enum StartBuildStatus
	{
		Success,
		EngineNotFound,
		AlreadyBuilding
	}

	public class EngineService : DisposableBase
	{
		private readonly EngineOptions _options;
		private readonly Dictionary<string, EngineRunner> _runners;
		private readonly AsyncReaderWriterLock _lock;
		private readonly IEngineRepository _engineRepo;
		private readonly IBuildRepository _buildRepo;
		private readonly ISmtModelFactory _smtModelFactory;
		private readonly IRuleEngineFactory _ruleEngineFactory;
		private readonly ITextCorpusFactory _textCorpusFactory;
		private readonly AsyncTimer _commitTimer;

		public EngineService(IOptions<EngineOptions> options, IEngineRepository engineRepo, IBuildRepository buildRepo,
			ISmtModelFactory smtModelFactory, IRuleEngineFactory ruleEngineFactory, ITextCorpusFactory textCorpusFactory)
		{
			_options = options.Value;
			_engineRepo = engineRepo;
			_buildRepo = buildRepo;
			_smtModelFactory = smtModelFactory;
			_ruleEngineFactory = ruleEngineFactory;
			_textCorpusFactory = textCorpusFactory;
			_runners = new Dictionary<string, EngineRunner>();
			_lock = new AsyncReaderWriterLock();
			_commitTimer = new AsyncTimer(EngineCommitAsync);

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
				foreach (EngineRunner runner in _runners.Values)
					await runner.CommitAsync();
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

		public async Task<(Engine Engine, bool ProjectAdded)> AddProjectAsync(string sourceLanguageTag, string targetLanguageTag,
			string projectId, bool isShared)
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				Engine engine = isShared ? await _engineRepo.GetByLanguageTagAsync(sourceLanguageTag, targetLanguageTag) : null;
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
					EngineRunner runner = CreateRunner(engine.Id);
					await runner.InitNewAsync();
				}
				else
				{
					// found an existing shared engine
					// TODO: should UpdateAsync determine if there is a conflict?
					if (engine.Projects.Contains(projectId))
						return (engine, false);
					engine.Projects.Add(projectId);
					await UpdateEngineAsync(engine);
				}

				return (engine, true);
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

				if (!engine.Projects.Remove(projectId))
					return false;

				if (engine.Projects.Count == 0)
				{
					// the engine has no associated projects so remove it
					await _engineRepo.DeleteAsync(engine);
					if (_runners.TryGetValue(engine.Id, out EngineRunner runner))
					{
						_runners.Remove(engine.Id);
						await runner.DeleteDataAsync();
						runner.Dispose();
					}
				}
				else
				{
					// engine still has associated projects so just update it
					await UpdateEngineAsync(engine);
				}
				return true;
			}
		}

		private async Task UpdateEngineAsync(Engine engine)
		{
			await _engineRepo.UpdateAsync(engine);
			// TODO: restart the build if it is already running
			if (!_runners.TryGetValue(engine.Id, out EngineRunner runner))
				runner = CreateRunner(engine.Id);
			await runner.StartBuildAsync(engine);
		}

		public async Task<(Build Build, StartBuildStatus Status)> StartBuildAsync(EngineLocatorType locatorType, string locator)
		{
			CheckDisposed();

			using (var upgradeKey = await _lock.UpgradeableReaderLockAsync())
			{
				Engine engine = await _engineRepo.GetByLocatorAsync(locatorType, locator);
				if (engine == null)
					return (null, StartBuildStatus.EngineNotFound);
				EngineRunner runner = await GetOrCreateRunnerAsync(upgradeKey, engine.Id);
				Build build = await runner.StartBuildAsync(engine);
				if (build == null)
					return (null, StartBuildStatus.AlreadyBuilding);
				return (build, StartBuildStatus.Success);
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
				if (_runners.TryGetValue(build.EngineId, out EngineRunner runner))
					await runner.CancelBuildAsync();
				return true;
			}
		}

		private async Task<EngineRunner> GetOrCreateRunnerAsync(AsyncReaderWriterLock.UpgradeableReaderKey upgradeKey,
			string engineId)
		{
			if (!_runners.TryGetValue(engineId, out EngineRunner runner))
			{
				using (await upgradeKey.UpgradeAsync())
					runner = CreateRunner(engineId);
			}
			return runner;
		}

		private EngineRunner CreateRunner(string engineId)
		{
			var runner = new EngineRunner(_buildRepo, _smtModelFactory, _ruleEngineFactory, _textCorpusFactory,
				_options.InactiveEngineTimeout, engineId);
			_runners[engineId] = runner;
			return runner;
		}

		protected override void DisposeManagedResources()
		{
			_commitTimer.Dispose();
			foreach (EngineRunner runner in _runners.Values)
				runner.Dispose();
			_runners.Clear();
		}
	}
}

