using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIL.Machine.Corpora;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Server.DataAccess;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Options;
using SIL.ObjectModel;
using SIL.Threading;

namespace SIL.Machine.WebApi.Server.Services
{
	public class EngineRunner : DisposableBase
	{
		private readonly IBuildRepository _buildRepo;
		private readonly ISmtModelFactory _smtModelFactory;
		private readonly IRuleEngineFactory _ruleEngineFactory;
		private readonly ITextCorpusFactory _textCorpusFactory;
		private readonly TimeSpan _inactiveTimeout;
		private readonly ILogger<EngineRunner> _logger;
		private readonly string _engineId;
		private readonly List<(IReadOnlyList<string> Source, IReadOnlyList<string> Target)> _trainedSegments;
		private readonly AsyncLock _lock;

		private IInteractiveSmtModel _smtModel;
		private IInteractiveSmtEngine _smtEngine;
		private ITranslationEngine _ruleEngine;
		private HybridTranslationEngine _hybridEngine;
		private CancellationTokenSource _cts;
		private ISmtBatchTrainer _batchTrainer;
		private Task _buildTask;
		private string _buildId;
		private bool _isUpdated;
		private DateTime _lastUsedTime;

		public EngineRunner(IOptions<EngineOptions> options, IBuildRepository buildRepo,
			ISmtModelFactory smtModelFactory, IRuleEngineFactory ruleEngineFactory,
			ITextCorpusFactory textCorpusFactory, ILogger<EngineRunner> logger, string engineId)
		{
			_buildRepo = buildRepo;
			_smtModelFactory = smtModelFactory;
			_ruleEngineFactory = ruleEngineFactory;
			_textCorpusFactory = textCorpusFactory;
			_inactiveTimeout = options.Value.InactiveEngineTimeout;
			_logger = logger;
			_engineId = engineId;
			_trainedSegments = new List<(IReadOnlyList<string> Source, IReadOnlyList<string> Target)>();
			_lock = new AsyncLock();
			_lastUsedTime = DateTime.Now;
		}

		internal bool IsLoaded { get; private set; }
		private bool IsBuilding => _buildTask != null && !_buildTask.IsCompleted;

		public async Task InitNewAsync()
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				_smtModelFactory.InitNewModel(_engineId);
				_ruleEngineFactory.InitNewEngine(_engineId);
			}
		}

		public async Task<TranslationResult> TranslateAsync(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				Load();

				TranslationResult result = _hybridEngine.Translate(segment);
				_lastUsedTime = DateTime.Now;
				return result;
			}
		}

		public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(int n, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				Load();

				IEnumerable<TranslationResult> results = _hybridEngine.Translate(n, segment);
				_lastUsedTime = DateTime.Now;
				return results.ToArray();
			}
		}

		public async Task<InteractiveTranslationResult> InteractiveTranslateAsync(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				Load();

				WordGraph smtWordGraph = _smtEngine.GetWordGraph(segment);
				TranslationResult ruleResult = _ruleEngine?.Translate(segment);
				_lastUsedTime = DateTime.Now;
				return new InteractiveTranslationResult(smtWordGraph, ruleResult);
			}
		}

		public async Task TrainSegmentPairAsync(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				Load();

				_hybridEngine.TrainSegment(sourceSegment, targetSegment);
				if (IsBuilding)
					_trainedSegments.Add((sourceSegment.ToArray(), targetSegment.ToArray()));
				_isUpdated = true;
				_lastUsedTime = DateTime.Now;
			}
		}

		public void RestartUnfinishedBuild(Build build, Engine engine)
		{
			CheckDisposed();

			using (_lock.Lock())
			{
				_buildId = build.Id;
				StartBuildInternal(engine, build);
			}
		}

		public async Task<Build> StartBuildAsync(Engine engine)
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				if (IsBuilding)
				{
					_cts.Cancel();
					await _buildRepo.DeleteAsync(_buildId);
					await _buildTask;
				}

				var build = new Build {EngineId = _engineId};
				await _buildRepo.InsertAsync(build);
				_buildId = build.Id;
				Build clone = build.Clone();
				StartBuildInternal(engine, build);
				return clone;
			}
		}

		private void StartBuildInternal(Engine engine, Build build)
		{
			Load();

			ITextCorpus sourceCorpus = _textCorpusFactory.Create(engine.Projects, TextCorpusType.Source);
			ITextCorpus targetCorpus = _textCorpusFactory.Create(engine.Projects, TextCorpusType.Target);
			_batchTrainer = _smtModel.CreateBatchTrainer(Preprocessors.Lowercase, sourceCorpus, Preprocessors.Lowercase,
				targetCorpus);
			_cts?.Dispose();
			_cts = new CancellationTokenSource();
			CancellationToken token = _cts.Token;
			_buildTask = Task.Run(() => BuildAsync(build, token), token);
			_lastUsedTime = DateTime.Now;
		}

		public async Task CancelBuildAsync()
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				if (IsBuilding)
				{
					_cts.Cancel();
					await _buildRepo.DeleteAsync(_buildId);
				}
			}
		}

		public async Task CommitAsync()
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				if (!IsLoaded || IsBuilding)
					return;

				if (DateTime.Now - _lastUsedTime > _inactiveTimeout)
					Unload();
				else
					SaveModel();
			}
		}

		internal void WaitForBuildToComplete()
		{
			CheckDisposed();

			if (_buildTask != null)
				Task.WaitAny(_buildTask);
			_buildTask = null;
		}

		public async Task DeleteDataAsync()
		{
			CheckDisposed();

			using (await _lock.LockAsync())
			{
				if (IsBuilding)
				{
					_cts.Cancel();
					await _buildRepo.DeleteAsync(_buildId);
				}
				Unload();
				_smtModelFactory.CleanupModel(_engineId);
				_ruleEngineFactory.CleanupEngine(_engineId);
			}
		}

		private void SaveModel()
		{
			if (_isUpdated)
			{
				_smtModel.Save();
				_isUpdated = false;
			}
		}

		private void Load()
		{
			if (IsLoaded)
				return;

			_smtModel = _smtModelFactory.Create(_engineId);
			_smtEngine = _smtModel.CreateInteractiveEngine();

			_ruleEngine = _ruleEngineFactory.Create(_engineId);

			_hybridEngine = new HybridTranslationEngine(_smtEngine, _ruleEngine);
			IsLoaded = true;
		}

		private void Unload()
		{
			if (!IsLoaded)
				return;

			SaveModel();

			_hybridEngine.Dispose();
			_hybridEngine = null;

			if (_ruleEngine != null)
			{
				_ruleEngine.Dispose();
				_ruleEngine = null;
			}

			_smtEngine.Dispose();
			_smtEngine = null;

			_smtModel.Dispose();
			_smtModel = null;
			IsLoaded = false;
		}

		private async Task BuildAsync(Build build, CancellationToken token)
		{
			try
			{
				_logger.LogInformation("Build starting ({0})", _engineId);
				var stopwatch = new Stopwatch();
				stopwatch.Start();
				var progress = new BuildProgress(_buildRepo, build);
				_batchTrainer.Train(progress, token.ThrowIfCancellationRequested);
				using (await _lock.LockAsync(token))
				{
					_batchTrainer.Save();
					foreach ((IReadOnlyList<string> Source, IReadOnlyList<string> Target) trainedSegment in _trainedSegments)
						_hybridEngine.TrainSegment(trainedSegment.Source, trainedSegment.Target);
					_trainedSegments.Clear();
					_batchTrainer.Dispose();
					_batchTrainer = null;
					await _buildRepo.DeleteAsync(build);
				}
				stopwatch.Stop();
				_logger.LogInformation("Build finished in {0}ms ({1})", stopwatch.Elapsed.TotalMilliseconds, _engineId);
			}
			catch (Exception e)
			{
				_logger.LogError(0, e, "Error occurred while building ({0})", _engineId);
				throw;
			}
		}

		protected override void DisposeManagedResources()
		{
			WaitForBuildToComplete();

			_batchTrainer?.Dispose();
			_batchTrainer = null;
			_cts?.Dispose();
			_cts = null;
			Unload();
			_trainedSegments.Clear();
		}
	}
}
