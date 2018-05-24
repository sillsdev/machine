using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Server.DataAccess;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Options;
using SIL.Machine.WebApi.Server.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Server.Services
{
	public class EngineRunner : DisposableBase
	{
		private const int MaxEnginePoolSize = 3;

		private readonly IEngineRepository _engineRepo;
		private readonly IBuildRepository _buildRepo;
		private readonly ISmtModelFactory _smtModelFactory;
		private readonly IRuleEngineFactory _ruleEngineFactory;
		private readonly ITextCorpusFactory _textCorpusFactory;
		private readonly IOptions<EngineOptions> _options;
		private readonly ILogger<EngineRunner> _logger;
		private readonly string _engineId;
		private readonly List<(IReadOnlyList<string> Source, IReadOnlyList<string> Target)> _trainedSegments;
		private readonly AsyncReaderWriterLock _lock;

		private Lazy<IInteractiveSmtModel> _smtModel;
		private ObjectPool<HybridTranslationEngine> _enginePool;
		private CancellationTokenSource _buildCts;
		private ISmtBatchTrainer _batchTrainer;
		private Task _buildTask;
		private string _buildId;
		private bool _isUpdated;
		private DateTime _lastUsedTime;
		private bool _isDisposing;

		public EngineRunner(IOptions<EngineOptions> options, IEngineRepository engineRepo, IBuildRepository buildRepo,
			ISmtModelFactory smtModelFactory, IRuleEngineFactory ruleEngineFactory,
			ITextCorpusFactory textCorpusFactory, ILogger<EngineRunner> logger, string engineId)
		{
			_engineRepo = engineRepo;
			_buildRepo = buildRepo;
			_smtModelFactory = smtModelFactory;
			_ruleEngineFactory = ruleEngineFactory;
			_textCorpusFactory = textCorpusFactory;
			_options = options;
			_logger = logger;
			_engineId = engineId;
			_trainedSegments = new List<(IReadOnlyList<string> Source, IReadOnlyList<string> Target)>();
			_lock = new AsyncReaderWriterLock();
			_smtModel = new Lazy<IInteractiveSmtModel>(CreateSmtModel);
			_enginePool = new ObjectPool<HybridTranslationEngine>(MaxEnginePoolSize, CreateEngine);
			_lastUsedTime = DateTime.Now;
		}

		internal bool IsLoaded => _smtModel.IsValueCreated;
		private bool IsBuilding => _buildTask != null && !_buildTask.IsCompleted;

		public async Task InitNewAsync()
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				_smtModelFactory.InitNewModel(_engineId);
				_ruleEngineFactory.InitNewEngine(_engineId);
			}
		}

		public async Task<TranslationResult> TranslateAsync(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync())
			{
				TranslationResult result = item.Object.Translate(segment);
				_lastUsedTime = DateTime.Now;
				return result;
			}
		}

		public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(int n, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync())
			{
				IEnumerable<TranslationResult> results = item.Object.Translate(n, segment);
				_lastUsedTime = DateTime.Now;
				return results.ToArray();
			}
		}

		public async Task<HybridInteractiveTranslationResult> InteractiveTranslateAsync(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			using (await _lock.ReaderLockAsync())
			using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync())
			{
				HybridInteractiveTranslationResult result = item.Object.TranslateInteractively(segment);
				_lastUsedTime = DateTime.Now;
				return result;
			}
		}

		public async Task TrainSegmentPairAsync(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync())
			{
				item.Object.TrainSegment(sourceSegment, targetSegment);
				if (IsBuilding)
					_trainedSegments.Add((sourceSegment.ToArray(), targetSegment.ToArray()));
				_isUpdated = true;
				_lastUsedTime = DateTime.Now;
			}
		}

		public async Task RestartUnfinishedBuildAsync(Build build, Engine engine)
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				_buildId = build.Id;
				await StartBuildInternalAsync(engine, build);
			}
		}

		public async Task<Build> StartBuildAsync(Engine engine)
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				if (IsBuilding)
				{
					_buildCts.Cancel();
					await _buildTask;
				}

				var build = new Build { EngineId = _engineId };
				await _buildRepo.InsertAsync(build);
				_buildId = build.Id;
				Build clone = build.Clone();
				await StartBuildInternalAsync(engine, build);
				return clone;
			}
		}

		private async Task StartBuildInternalAsync(Engine engine, Build build)
		{
			ITextCorpus sourceCorpus = await _textCorpusFactory.CreateAsync(engine.Projects, TextCorpusType.Source);
			ITextCorpus targetCorpus = await _textCorpusFactory.CreateAsync(engine.Projects, TextCorpusType.Target);
			_batchTrainer = _smtModel.Value.CreateBatchTrainer(Preprocessors.Lowercase, sourceCorpus,
				Preprocessors.Lowercase, targetCorpus);
			_buildCts?.Dispose();
			_buildCts = new CancellationTokenSource();
			CancellationToken token = _buildCts.Token;
			_buildTask = Task.Run(() => BuildAsync(build, token), token);
			_lastUsedTime = DateTime.Now;
		}

		public async Task CancelBuildAsync()
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				if (IsBuilding)
					_buildCts.Cancel();
			}
		}

		public async Task CommitAsync()
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				if (!IsLoaded || IsBuilding)
					return;

				if (DateTime.Now - _lastUsedTime > _options.Value.InactiveEngineTimeout)
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

			using (await _lock.WriterLockAsync())
			{
				if (IsBuilding)
					_buildCts.Cancel();
				Unload();
				_smtModelFactory.CleanupModel(_engineId);
				_ruleEngineFactory.CleanupEngine(_engineId);
			}
		}

		private void SaveModel()
		{
			if (_isUpdated)
			{
				_smtModel.Value.Save();
				_isUpdated = false;
			}
		}

		private void Unload()
		{
			if (!IsLoaded)
				return;

			SaveModel();

			_enginePool.Dispose();
			_smtModel.Value.Dispose();

			_smtModel = new Lazy<IInteractiveSmtModel>(CreateSmtModel);
			_enginePool = new ObjectPool<HybridTranslationEngine>(MaxEnginePoolSize, CreateEngine);
		}

		private IInteractiveSmtModel CreateSmtModel()
		{
			return _smtModelFactory.Create(_engineId);
		}

		private Task<HybridTranslationEngine> CreateEngine()
		{
			IInteractiveSmtEngine smtEngine = _smtModel.Value.CreateInteractiveEngine();
			ITranslationEngine ruleEngine = _ruleEngineFactory.Create(_engineId);
			return Task.FromResult(new HybridTranslationEngine(smtEngine, ruleEngine));
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
				using (await _lock.WriterLockAsync(token))
				using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync())
				{
					_batchTrainer.Save();
					foreach ((IReadOnlyList<string> source, IReadOnlyList<string> target) in _trainedSegments)
						item.Object.TrainSegment(source, target);
					_trainedSegments.Clear();

					Engine engine = await _engineRepo.GetAsync(_engineId);
					if (engine != null)
					{
						await _engineRepo.ConcurrentUpdateAsync(engine,
							e => e.Confidence = _batchTrainer.Stats.TranslationModelBleu);
					}

					_batchTrainer.Dispose();
					_batchTrainer = null;
					build.State = BuildStates.Completed;
					build.DateFinished = DateTime.UtcNow;
					await _buildRepo.UpdateAsync(build);
				}
				stopwatch.Stop();
				_logger.LogInformation("Build finished in {0}ms ({1})", stopwatch.Elapsed.TotalMilliseconds, _engineId);
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Build canceled ({1})", _engineId);
				if (!_isDisposing)
				{
					build.State = BuildStates.Canceled;
					build.DateFinished = DateTime.UtcNow;
					await _buildRepo.UpdateAsync(build);
				}
			}
			catch (Exception e)
			{
				_logger.LogError(0, e, "Error occurred while building ({0})", _engineId);
				build.State = BuildStates.Faulted;
				build.Message = e.Message;
				build.DateFinished = DateTime.UtcNow;
				await _buildRepo.UpdateAsync(build);
			}
		}

		protected override void DisposeManagedResources()
		{
			_isDisposing = true;
			if (IsBuilding)
			{
				_buildCts.Cancel();
				WaitForBuildToComplete();
			}

			_batchTrainer?.Dispose();
			_batchTrainer = null;
			_buildCts?.Dispose();
			_buildCts = null;
			Unload();
			_trainedSegments.Clear();
		}
	}
}
