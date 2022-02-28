namespace SIL.Machine.WebApi.Services;

internal class SmtTransferEngineRuntime : AsyncDisposableBase, IEngineRuntime
{
	public class Factory : EngineRuntimeFactory<SmtTransferEngineRuntime>
	{
		public Factory(IServiceProvider serviceProvider)
			: base(serviceProvider, EngineType.SmtTransfer)
		{
		}
	}

	private const int MaxEnginePoolSize = 3;

	private readonly IRepository<Engine> _engines;
	private readonly IRepository<Build> _builds;
	private readonly ISmtModelFactory _smtModelFactory;
	private readonly ITransferEngineFactory _transferEngineFactory;
	private readonly ITruecaserFactory _truecaserFactory;
	private readonly IOptions<EngineOptions> _engineOptions;
	private readonly ILogger<SmtTransferEngineRuntime> _logger;
	private readonly IBackgroundJobClient _jobClient;
	private readonly string _engineId;
	private readonly List<SegmentPair> _trainedSegments;
	private readonly AsyncReaderWriterLock _lock;
	private readonly AsyncManualResetEvent _buildFinishedEvent;
	private readonly IDataFileService _dataFileService;

	private Lazy<IInteractiveTranslationModel> _smtModel;
	private ObjectPool<HybridTranslationEngine> _enginePool;
	private AsyncLazy<ITruecaser> _truecaser;
	private CancellationTokenSource _buildCts;
	private bool _isUpdated;
	private DateTime _lastUsedTime;

	public SmtTransferEngineRuntime(IOptions<EngineOptions> engineOptions, IRepository<Engine> engines,
		IRepository<Build> builds, ISmtModelFactory smtModelFactory, ITransferEngineFactory transferEngineFactory,
		ITruecaserFactory truecaserFactory, IBackgroundJobClient jobClient, IDataFileService dataFileService,
		ILogger<SmtTransferEngineRuntime> logger, string engineId)
	{
		_engines = engines;
		_builds = builds;
		_smtModelFactory = smtModelFactory;
		_transferEngineFactory = transferEngineFactory;
		_truecaserFactory = truecaserFactory;
		_engineOptions = engineOptions;
		_logger = logger;
		_jobClient = jobClient;
		_dataFileService = dataFileService;
		_engineId = engineId;
		_trainedSegments = new List<SegmentPair>();
		_lock = new AsyncReaderWriterLock();
		_smtModel = new Lazy<IInteractiveTranslationModel>(CreateSmtModel);
		_enginePool = new ObjectPool<HybridTranslationEngine>(MaxEnginePoolSize, CreateEngine);
		_truecaser = new AsyncLazy<ITruecaser>(CreateTruecaserAsync);
		_buildFinishedEvent = new AsyncManualResetEvent(true);
		_lastUsedTime = DateTime.Now;
		_buildCts = new CancellationTokenSource();
	}

	internal bool IsLoaded => _smtModel.IsValueCreated;
	private bool IsBuilding => !_buildFinishedEvent.IsSet;

	public async Task InitNewAsync()
	{
		CheckDisposed();

		using (await _lock.WriterLockAsync())
		{
			_smtModelFactory.InitNew(_engineId);
			_transferEngineFactory.InitNew(_engineId);
		}
	}

	public async Task<TranslationResult> TranslateAsync(IReadOnlyList<string> segment)
	{
		CheckDisposed();

		IReadOnlyList<string> preprocSegment = TokenProcessors.Lowercase.Process(segment);

		using (await _lock.ReaderLockAsync())
		using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync())
		{
			TranslationResult result = item.Object.Translate(preprocSegment);
			result = (await _truecaser).Truecase(segment, result);
			_lastUsedTime = DateTime.Now;
			return result;
		}
	}

	public async Task<IReadOnlyList<TranslationResult>> TranslateAsync(int n, IReadOnlyList<string> segment)
	{
		CheckDisposed();

		IReadOnlyList<string> preprocSegment = TokenProcessors.Lowercase.Process(segment);

		using (await _lock.ReaderLockAsync())
		using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync())
		{
			ITruecaser truecaser = await _truecaser;
			var results = new List<TranslationResult>();
			foreach (TranslationResult result in item.Object.Translate(n, preprocSegment))
				results.Add(truecaser.Truecase(segment, result));
			_lastUsedTime = DateTime.Now;
			return results;
		}
	}

	public async Task<WordGraph> GetWordGraphAsync(IReadOnlyList<string> segment)
	{
		CheckDisposed();

		IReadOnlyList<string> preprocSegment = TokenProcessors.Lowercase.Process(segment);

		using (await _lock.ReaderLockAsync())
		using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync())
		{
			WordGraph result = item.Object.GetWordGraph(preprocSegment);
			result = (await _truecaser).Truecase(segment, result);
			_lastUsedTime = DateTime.Now;
			return result;
		}
	}

	public async Task TrainSegmentPairAsync(IReadOnlyList<string> sourceSegment,
		IReadOnlyList<string> targetSegment, bool sentenceStart)
	{
		CheckDisposed();

		IReadOnlyList<string> preprocSourceSegment = TokenProcessors.Lowercase.Process(sourceSegment);
		IReadOnlyList<string> preprocTargetSegment = TokenProcessors.Lowercase.Process(targetSegment);

		using (await _lock.WriterLockAsync())
		using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync())
		{
			item.Object.TrainSegment(preprocSourceSegment, preprocTargetSegment);
			if (IsBuilding)
			{
				_trainedSegments.Add(new SegmentPair
				{
					Source = sourceSegment,
					Target = targetSegment,
					SentenceStart = sentenceStart
				});
			}
			await _engines.UpdateAsync(_engineId, u => u.Inc(e => e.TrainedSegmentCount, 1));
			(await _truecaser).TrainSegment(targetSegment, sentenceStart);
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
			await _builds.InsertAsync(build);
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
				await UnloadAsync();
			else
				await SaveModelAsync();
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

			await UnloadAsync();
			_smtModelFactory.Cleanup(_engineId);
			_transferEngineFactory.Cleanup(_engineId);
			_truecaserFactory.Cleanup(_engineId);
		}
	}

	private async Task CancelBuildInternalAsync()
	{
		Build? build = await _builds.GetByEngineIdAsync(_engineId);
		if (build == null)
			return;
		if (build.State == BuildState.Pending)
		{
			// if the build is pending, then delete it
			// the job will still run, but it will exit before performing the build
			await _builds.DeleteAsync(build);
		}
		else if (build.State == BuildState.Active && !IsBuilding)
		{
			// if the build is active but not actually running yet, then change the state to canceled
			// the job will still run, but it will exit before performing the build
			// this should not happen, but check for it just in case
			await _builds.UpdateAsync(build, u => u
				.Inc(b => b.Revision)
				.Set(b => b.State, BuildState.Canceled)
				.Set(b => b.DateFinished, DateTime.UtcNow));
		}
		else if (IsBuilding)
		{
			// if the build is actually running, then cancel it
			_buildCts.Cancel();
		}
	}

	private async Task SaveModelAsync()
	{
		if (_isUpdated)
		{
			_smtModel.Value.Save();
			ITruecaser truecaser = await _truecaser;
			await truecaser.SaveAsync();
			_isUpdated = false;
		}
	}

	private async Task UnloadAsync()
	{
		if (!IsLoaded)
			return;

		await SaveModelAsync();

		_enginePool.Dispose();
		_smtModel.Value.Dispose();

		_smtModel = new Lazy<IInteractiveTranslationModel>(CreateSmtModel);
		_enginePool = new ObjectPool<HybridTranslationEngine>(MaxEnginePoolSize, CreateEngine);
		_truecaser = new AsyncLazy<ITruecaser>(CreateTruecaserAsync);
	}

	private IInteractiveTranslationModel CreateSmtModel()
	{
		return _smtModelFactory.Create(_engineId);
	}

	private HybridTranslationEngine CreateEngine()
	{
		IInteractiveTranslationEngine interactiveEngine = _smtModel.Value.CreateInteractiveEngine();
		ITranslationEngine? ruleEngine = _transferEngineFactory.Create(_engineId);
		return new HybridTranslationEngine(interactiveEngine, ruleEngine);
	}

	private Task<ITruecaser> CreateTruecaserAsync()
	{
		return _truecaserFactory.CreateAsync(_engineId)!;
	}

	private async Task BuildAsync(Engine engine, IBuildHandler buildHandler, IJobCancellationToken jobToken)
	{
		Build? build = null;
		ITrainer? modelTrainer = null;
		ITrainer? truecaseTrainer = null;
		CancellationTokenSource? cts = null;
		try
		{
			var stopwatch = new Stopwatch();
			ITruecaser truecaser = await _truecaser;
			using (await _lock.WriterLockAsync(jobToken.ShutdownToken))
			{
				build = await _builds.GetByEngineIdAsync(_engineId);
				// if the build is not found, then there are no pending or active builds for this engine, so exit
				if (build == null)
					return;

				await buildHandler.OnStarted(new BuildContext(engine, build));
				_logger.LogInformation("Build started ({0})", _engineId);
				stopwatch.Start();

				if (build.State == BuildState.Pending)
				{
					build = (await _builds.UpdateAsync(build, u => u
						.Inc(b => b.Revision)
						.Set(b => b.State, BuildState.Active)))!;
				}

				var tokenizer = new LatinWordTokenizer();
				ITextCorpus sourceCorpus = await _dataFileService.CreateTextCorpusAsync(engine.Id, CorpusType.Source,
					tokenizer);
				ITextCorpus targetCorpus = await _dataFileService.CreateTextCorpusAsync(engine.Id, CorpusType.Target,
					tokenizer);
				var corpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
				modelTrainer = _smtModel.Value.CreateTrainer(corpus, TokenProcessors.Lowercase,
					TokenProcessors.Lowercase);
				truecaseTrainer = truecaser.CreateTrainer(targetCorpus);

				_buildCts.Dispose();
				_buildCts = new CancellationTokenSource();
				_buildFinishedEvent.Reset();

				cts = CancellationTokenSource.CreateLinkedTokenSource(_buildCts.Token, jobToken.ShutdownToken);
			}

			CancellationToken token = cts.Token;
			var progress = new BuildProgress(_builds, build);
			modelTrainer.Train(progress, token.ThrowIfCancellationRequested);
			truecaseTrainer.Train(checkCanceled: token.ThrowIfCancellationRequested);

			using (await _lock.WriterLockAsync(token))
			using (ObjectPoolItem<HybridTranslationEngine> item = await _enginePool.GetAsync(token))
			{
				token.ThrowIfCancellationRequested();
				modelTrainer.Save();
				await truecaseTrainer.SaveAsync();
				foreach (SegmentPair pair in _trainedSegments)
				{
					item.Object.TrainSegment(TokenProcessors.Lowercase.Process(pair.Source),
						TokenProcessors.Lowercase.Process(pair.Target));
					truecaser.TrainSegment(pair.Target, pair.SentenceStart);
				}

				engine = (await _engines.UpdateAsync(engine, u => u
					.Set(e => e.Confidence, modelTrainer.Stats.Metrics["bleu"])
					.Set(e => e.TrainedSegmentCount, modelTrainer.Stats.TrainedSegmentCount + _trainedSegments.Count)))!;
				_trainedSegments.Clear();
			}

			build = (await _builds.UpdateAsync(build, u => u
				.Inc(b => b.Revision)
				.Set(b => b.State, BuildState.Completed)
				.Set(b => b.DateFinished, DateTime.UtcNow)))!;
			stopwatch.Stop();
			_logger.LogInformation("Build completed in {0}ms ({1})", stopwatch.Elapsed.TotalMilliseconds, _engineId);
			await buildHandler.OnCompleted(new BuildContext(engine, build));
		}
		catch (OperationCanceledException)
		{
			if (build != null)
			{
				// this job is canceled because of a shutdown, pass on the exception, so it will stay in the queue
				if (jobToken.ShutdownToken.IsCancellationRequested)
				{
					// switch state back to pending
					await _builds.UpdateAsync(build, u => u
						.Inc(b => b.Revision)
						.Set(b => b.Message, null)
						.Set(b => b.PercentCompleted, 0)
						.Set(b => b.State, BuildState.Pending));
					throw;
				}

				build = (await _builds.UpdateAsync(build, u => u
					.Inc(b => b.Revision)
					.Set(b => b.State, BuildState.Canceled)
					.Set(b => b.DateFinished, DateTime.UtcNow)))!;
				_logger.LogInformation("Build canceled ({0})", _engineId);
				await buildHandler.OnCanceled(new BuildContext(engine, build));
			}
		}
		catch (Exception e)
		{
			if (build != null)
			{
				build = (await _builds.UpdateAsync(build, u => u
					.Inc(b => b.Revision)
					.Set(b => b.State, BuildState.Faulted)
					.Set(b => b.Message, e.Message)
					.Set(b => b.DateFinished, DateTime.UtcNow)))!;
				_logger.LogError(0, e, "Build faulted ({0})", _engineId);
				await buildHandler.OnFailed(new BuildContext(engine, build));
			}
			throw;
		}
		finally
		{
			modelTrainer?.Dispose();
			truecaseTrainer?.Dispose();
			_buildFinishedEvent.Set();
			cts?.Dispose();
		}
	}

	protected override async ValueTask DisposeAsyncCore()
	{
		_buildCts.Dispose();
		await UnloadAsync();
		_trainedSegments.Clear();
	}

	internal class BuildRunner
	{
		private readonly IEngineServiceInternal _engineService;
		private readonly IBuildHandler _buildHandler;

		public BuildRunner(IEngineServiceInternal engineService, IBuildHandler buildHandler)
		{
			_engineService = engineService;
			_buildHandler = buildHandler;
		}

		[AutomaticRetry(Attempts = 0)]
		public async Task RunAsync(string engineId, IJobCancellationToken jobToken)
		{
			(Engine? engine, IEngineRuntime? runtime) = await _engineService.GetEngineAsync(engineId);
			// the engine was removed after we enqueued the job, so exit
			if (engine == null || runtime == null)
				return;

			await ((SmtTransferEngineRuntime)runtime).BuildAsync(engine, _buildHandler, jobToken);
		}
	}

	private struct SegmentPair
	{
		public IReadOnlyList<string> Source { get; set; }
		public IReadOnlyList<string> Target { get; set; }
		public bool SentenceStart { get; set; }
	}
}
