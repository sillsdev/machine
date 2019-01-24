using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIL.Machine.Corpora;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Configuration;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;
using SIL.Machine.WebApi.Utils;
using SIL.ObjectModel;

namespace SIL.Machine.WebApi.Services
{
	public class EngineRuntime : DisposableBase
	{
		private const int MaxEnginePoolSize = 3;

		private readonly IEngineRepository _engineRepo;
		private readonly IBuildRepository _buildRepo;
		private readonly ISmtModelFactory _smtModelFactory;
		private readonly IRuleEngineFactory _ruleEngineFactory;
		private readonly ITextCorpusFactory _textCorpusFactory;
		private readonly IOptions<EngineOptions> _engineOptions;
		private readonly ILogger<EngineRuntime> _logger;
		private readonly IBackgroundJobClient _jobClient;
		private readonly string _engineId;
		private readonly List<(IReadOnlyList<string> Source, IReadOnlyList<string> Target)> _trainedSegments;
		private readonly AsyncReaderWriterLock _lock;
		private readonly AsyncManualResetEvent _buildFinishedEvent;

		private Lazy<IInteractiveSmtModel> _smtModel;
		private ObjectPool<HybridTranslationEngine> _enginePool;
		private CancellationTokenSource _buildCts;
		private bool _isUpdated;
		private DateTime _lastUsedTime;

		public EngineRuntime(IOptions<EngineOptions> engineOptions, IEngineRepository engineRepo,
			IBuildRepository buildRepo, ISmtModelFactory smtModelFactory, IRuleEngineFactory ruleEngineFactory,
			IBackgroundJobClient jobClient, ITextCorpusFactory textCorpusFactory, ILogger<EngineRuntime> logger,
			string engineId)
		{
			_engineRepo = engineRepo;
			_buildRepo = buildRepo;
			_smtModelFactory = smtModelFactory;
			_ruleEngineFactory = ruleEngineFactory;
			_textCorpusFactory = textCorpusFactory;
			_engineOptions = engineOptions;
			_logger = logger;
			_jobClient = jobClient;
			_engineId = engineId;
			_trainedSegments = new List<(IReadOnlyList<string> Source, IReadOnlyList<string> Target)>();
			_lock = new AsyncReaderWriterLock();
			_smtModel = new Lazy<IInteractiveSmtModel>(CreateSmtModel);
			_enginePool = new ObjectPool<HybridTranslationEngine>(MaxEnginePoolSize, CreateEngine);
			_buildFinishedEvent = new AsyncManualResetEvent(true);
			_lastUsedTime = DateTime.Now;
		}

		internal bool IsLoaded => _smtModel.IsValueCreated;
		private bool IsBuilding => !_buildFinishedEvent.IsSet;

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

			IReadOnlyList<string> preprocSegment = segment.Preprocess(Preprocessors.Lowercase);

			using (await _lock.ReaderLockAsync())
			using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync())
			{
				TranslationResult result = item.Object.Translate(preprocSegment);
				_lastUsedTime = DateTime.Now;
				return result;
			}
		}

		public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(int n, IReadOnlyList<string> segment)
		{
			CheckDisposed();

			IReadOnlyList<string> preprocSegment = segment.Preprocess(Preprocessors.Lowercase);

			using (await _lock.ReaderLockAsync())
			using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync())
			{
				IEnumerable<TranslationResult> results = item.Object.Translate(n, preprocSegment);
				_lastUsedTime = DateTime.Now;
				return results.ToArray();
			}
		}

		public async Task<HybridInteractiveTranslationResult> InteractiveTranslateAsync(IReadOnlyList<string> segment)
		{
			CheckDisposed();

			IReadOnlyList<string> preprocSegment = segment.Preprocess(Preprocessors.Lowercase);

			using (await _lock.ReaderLockAsync())
			using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync())
			{
				HybridInteractiveTranslationResult result = item.Object.TranslateInteractively(preprocSegment);
				_lastUsedTime = DateTime.Now;
				return result;
			}
		}

		public async Task TrainSegmentPairAsync(IReadOnlyList<string> sourceSegment,
			IReadOnlyList<string> targetSegment)
		{
			CheckDisposed();

			IReadOnlyList<string> preprocSourceSegment = sourceSegment.Preprocess(Preprocessors.Lowercase);
			IReadOnlyList<string> preprocTargetSegment = targetSegment.Preprocess(Preprocessors.Lowercase);

			using (await _lock.WriterLockAsync())
			using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync())
			{
				item.Object.TrainSegment(preprocSourceSegment, preprocTargetSegment);
				if (IsBuilding)
					_trainedSegments.Add((preprocSourceSegment, preprocTargetSegment));
				_isUpdated = true;
				_lastUsedTime = DateTime.Now;
			}
		}

		public async Task<Build> StartBuildAsync()
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				// cancel the existing build before starting a new build
				await CancelBuildInternalAsync();
				await _buildFinishedEvent.WaitAsync();

				var build = new Build { EngineRef = _engineId };
				await _buildRepo.InsertAsync(build);
				_jobClient.Enqueue<BuildRunner>(r => r.RunAsync(_engineId, JobCancellationToken.Null));
				_lastUsedTime = DateTime.Now;
				return build;
			}
		}

		public async Task CancelBuildAsync()
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
				await CancelBuildInternalAsync();
		}

		public async Task CommitAsync()
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				if (!IsLoaded || IsBuilding)
					return;

				if (DateTime.Now - _lastUsedTime > _engineOptions.Value.InactiveEngineTimeout)
					Unload();
				else
					SaveModel();
			}
		}

		public async Task DeleteDataAsync()
		{
			CheckDisposed();

			using (await _lock.WriterLockAsync())
			{
				// ensure that there is no build running before unloading
				await CancelBuildInternalAsync();
				await _buildFinishedEvent.WaitAsync();

				Unload();
				_smtModelFactory.CleanupModel(_engineId);
				_ruleEngineFactory.CleanupEngine(_engineId);
			}
		}

		private async Task CancelBuildInternalAsync()
		{
			Build build = await _buildRepo.GetByEngineIdAsync(_engineId);
			if (build == null)
				return;
			if (build.State == BuildStates.Pending)
			{
				// if the build is pending, then delete it
				// the job will still run, but it will exit before performing the build 
				await _buildRepo.DeleteAsync(build);
			}
			else if (build.State == BuildStates.Active && !IsBuilding)
			{
				// if the build is active but not actually running yet, then change the state to canceled
				// the job will still run, but it will exit before performing the build
				// this should not happen, but check for it just in case
				build.State = BuildStates.Canceled;
				build.DateFinished = DateTime.UtcNow;
				await _buildRepo.UpdateAsync(build);
			}
			else if (IsBuilding)
			{
				// if the build is actually running, then cancel it
				_buildCts.Cancel();
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

		private async Task BuildAsync(IReadOnlyCollection<string> projects, IJobCancellationToken jobToken)
		{
			Build build = null;
			ISmtBatchTrainer trainer = null;
			CancellationTokenSource cts = null;
			try
			{
				var stopwatch = new Stopwatch();
				using (await _lock.WriterLockAsync(jobToken.ShutdownToken))
				{
					build = await _buildRepo.GetByEngineIdAsync(_engineId);
					// if the build is not found, then there are no pending or active builds for this engine, so exit
					if (build == null)
						return;

					_logger.LogInformation("Build started ({0})", _engineId);
					stopwatch.Start();

					if (build.State == BuildStates.Pending)
					{
						build.State = BuildStates.Active;
						await _buildRepo.UpdateAsync(build);
					}

					ITextCorpus sourceCorpus = await _textCorpusFactory.CreateAsync(projects, TextCorpusType.Source);
					ITextCorpus targetCorpus = await _textCorpusFactory.CreateAsync(projects, TextCorpusType.Target);
					trainer = _smtModel.Value.CreateBatchTrainer(Preprocessors.Lowercase, sourceCorpus,
						Preprocessors.Lowercase, targetCorpus);

					_buildCts?.Dispose();
					_buildCts = new CancellationTokenSource();
					_buildFinishedEvent.Reset();

					cts = CancellationTokenSource.CreateLinkedTokenSource(_buildCts.Token, jobToken.ShutdownToken);
				}

				CancellationToken token = cts.Token;
				var progress = new BuildProgress(_buildRepo, build);
				trainer.Train(progress, token.ThrowIfCancellationRequested);
				using (await _lock.WriterLockAsync(token))
				using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync(token))
				{
					token.ThrowIfCancellationRequested();
					trainer.Save();
					foreach ((IReadOnlyList<string> source, IReadOnlyList<string> target) in _trainedSegments)
						item.Object.TrainSegment(source, target);
					_trainedSegments.Clear();

					await _engineRepo.ConcurrentUpdateAsync(_engineId,
						e => e.Confidence = trainer.Stats.TranslationModelBleu);
				}

				build.State = BuildStates.Completed;
				build.DateFinished = DateTime.UtcNow;
				await _buildRepo.UpdateAsync(build);
				stopwatch.Stop();
				_logger.LogInformation("Build completed in {0}ms ({1})", stopwatch.Elapsed.TotalMilliseconds,
					_engineId);
			}
			catch (OperationCanceledException)
			{
				// this job is canceled because of a shutdown, pass on the exception, so it will stay in the queue
				if (jobToken.ShutdownToken.IsCancellationRequested)
				{
					// switch state back to pending
					build.Message = null;
					build.PercentCompleted = 0;
					build.State = BuildStates.Pending;
					await _buildRepo.UpdateAsync(build);
					throw;
				}

				build.State = BuildStates.Canceled;
				build.DateFinished = DateTime.UtcNow;
				await _buildRepo.UpdateAsync(build);
				_logger.LogInformation("Build canceled ({1})", _engineId);
			}
			catch (Exception e)
			{
				if (build != null)
				{
					build.State = BuildStates.Faulted;
					build.Message = e.Message;
					build.DateFinished = DateTime.UtcNow;
					await _buildRepo.UpdateAsync(build);
					_logger.LogError(0, e, "Build faulted ({0})", _engineId);
				}
				throw;
			}
			finally
			{
				trainer?.Dispose();
				_buildFinishedEvent.Set();
				cts?.Dispose();
			}
		}

		protected override void DisposeManagedResources()
		{
			_buildCts?.Dispose();
			Unload();
			_trainedSegments.Clear();
		}

		internal class BuildRunner
		{
			private readonly EngineService _engineService;

			public BuildRunner(EngineService engineService)
			{
				_engineService = engineService;
			}

			[AutomaticRetry(Attempts = 0)]
			public async Task RunAsync(string engineId, IJobCancellationToken jobToken)
			{
				(Engine engine, EngineRuntime runtime) = await _engineService.GetEngineAsync(engineId);
				// the engine was removed after we enqueued the job, so exit
				if (engine == null)
					return;

				await runtime.BuildAsync(engine.Projects, jobToken);
			}
		}
	}
}
