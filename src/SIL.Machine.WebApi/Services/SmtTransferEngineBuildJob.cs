namespace SIL.Machine.WebApi.Services;

public class SmtTransferEngineBuildJob
{
	private readonly IRepository<Engine> _engines;
	private readonly IRepository<Build> _builds;
	private readonly IRepository<TrainSegmentPair> _trainSegmentPairs;
	private readonly IDistributedReaderWriterLockFactory _lockFactory;
	private readonly IDataFileService _dataFileService;
	private readonly ITruecaserFactory _truecaserFactory;
	private readonly ISmtModelFactory _smtModelFactory;

	private readonly ILogger<SmtTransferEngineBuildJob> _logger;
	private readonly IWebhookService _webhookService;

	public SmtTransferEngineBuildJob(IRepository<Engine> engines, IRepository<Build> builds,
		IRepository<TrainSegmentPair> trainSegmentPairs, IDistributedReaderWriterLockFactory lockFactory,
		IDataFileService dataFileService, ITruecaserFactory truecaserFactory, ISmtModelFactory smtModelFactory,
		ILogger<SmtTransferEngineBuildJob> logger, IWebhookService webhookService)
	{
		_engines = engines;
		_builds = builds;
		_trainSegmentPairs = trainSegmentPairs;
		_lockFactory = lockFactory;
		_dataFileService = dataFileService;
		_truecaserFactory = truecaserFactory;
		_smtModelFactory = smtModelFactory;
		_logger = logger;
		_webhookService = webhookService;
	}

	[AutomaticRetry(Attempts = 0)]
	public async Task RunAsync(string engineId, string buildId, PerformContext performContext,
		CancellationToken cancellationToken)
	{
		Engine? engine = await _engines.GetAsync(engineId, cancellationToken);
		// the engine was removed after we enqueued the job, so exit
		if (engine == null)
			return;

		IDistributedReaderWriterLock rwLock = _lockFactory.Create(engineId);

		Build? build = null;
		ITrainer? smtModelTrainer = null;
		ITrainer? truecaseTrainer = null;
		try
		{
			var stopwatch = new Stopwatch();
			await using (await rwLock.WriterLockAsync(cancellationToken: cancellationToken))
			{
				build = await _builds.GetAsync(buildId, cancellationToken);
				// the engine was removed after we enqueued the job, so exit
				if (build == null)
					return;

				if (build.State == BuildState.Canceled)
					throw new OperationCanceledException();

				await _trainSegmentPairs.DeleteAllAsync(p => p.EngineRef == engineId, cancellationToken);

				build.State = BuildState.Active;
				await _webhookService.TriggerEventAsync(WebhookEvent.BuildStarted, engine.Owner, build);
				_logger.LogInformation("Build started ({0})", engineId);
				stopwatch.Start();

				build = (await _builds.UpdateAsync(build, u => u
					.Set(b => b.State, BuildState.Active)
					.Set(b => b.JobId, performContext.BackgroundJob.Id), cancellationToken: CancellationToken.None))!;
				engine = (await _engines.UpdateAsync(engine, u => u.Set(e => e.IsBuilding, true),
					cancellationToken: CancellationToken.None))!;

				var tokenizer = new LatinWordTokenizer();
				ITextCorpus sourceCorpus = await _dataFileService.CreateTextCorpusAsync(engine.Id, CorpusType.Source,
					tokenizer);
				ITextCorpus targetCorpus = await _dataFileService.CreateTextCorpusAsync(engine.Id, CorpusType.Target,
					tokenizer);
				var corpus = new ParallelTextCorpus(sourceCorpus, targetCorpus);
				smtModelTrainer = _smtModelFactory.CreateTrainer(engineId, corpus, TokenProcessors.Lowercase,
					TokenProcessors.Lowercase);
				truecaseTrainer = _truecaserFactory.CreateTrainer(engineId, targetCorpus);
			}

			var progress = new BuildProgress(_builds, build);
			smtModelTrainer.Train(progress, cancellationToken.ThrowIfCancellationRequested);
			truecaseTrainer.Train(checkCanceled: cancellationToken.ThrowIfCancellationRequested);

			await using (await rwLock.WriterLockAsync(cancellationToken: cancellationToken))
			{
				cancellationToken.ThrowIfCancellationRequested();
				smtModelTrainer.Save();
				await truecaseTrainer.SaveAsync();
				ITruecaser truecaser = await _truecaserFactory.CreateAsync(engineId);
				IReadOnlyList<TrainSegmentPair> segmentPairs = await _trainSegmentPairs
					.GetAllAsync(p => p.EngineRef == engineId, CancellationToken.None);
				using (IInteractiveTranslationModel smtModel = _smtModelFactory.Create(engineId))
				using (IInteractiveTranslationEngine smtEngine = smtModel.CreateInteractiveEngine())
				{
					foreach (TrainSegmentPair segmentPair in segmentPairs)
					{
						smtEngine.TrainSegment(TokenProcessors.Lowercase.Process(segmentPair.Source),
							TokenProcessors.Lowercase.Process(segmentPair.Target));
						truecaser.TrainSegment(segmentPair.Target, segmentPair.SentenceStart);
					}
				}

				engine = (await _engines.UpdateAsync(engine, u => u
					.Set(e => e.Confidence, smtModelTrainer.Stats.Metrics["bleu"])
					.Set(e => e.TrainedSegmentCount, smtModelTrainer.Stats.TrainedSegmentCount + segmentPairs.Count)
					.Set(e => e.IsBuilding, false)
					.Inc(e => e.BuildRevision),
					cancellationToken: CancellationToken.None))!;
			}

			build = (await _builds.UpdateAsync(build, u => u
				.Set(b => b.State, BuildState.Completed)
				.Set(b => b.DateFinished, DateTime.UtcNow), cancellationToken: CancellationToken.None))!;
			stopwatch.Stop();
			_logger.LogInformation("Build completed in {0}ms ({1})", stopwatch.Elapsed.TotalMilliseconds, engineId);
			await _webhookService.TriggerEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);
		}
		catch (OperationCanceledException)
		{
			await using (await rwLock.WriterLockAsync(cancellationToken: cancellationToken))
			{
				engine = (await _engines.UpdateAsync(engine, u => u.Set(e => e.IsBuilding, false),
					cancellationToken: CancellationToken.None))!;
			}

			build = await _builds.GetAsync(buildId, CancellationToken.None);
			if (build == null)
				throw;

			if (build.State == BuildState.Canceled)
			{
				build = (await _builds.UpdateAsync(build, u => u
					.Set(b => b.DateFinished, DateTime.UtcNow), cancellationToken: CancellationToken.None))!;
				_logger.LogInformation("Build canceled ({0})", engineId);
				await _webhookService.TriggerEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);
			}
			else
			{
				// the build was canceled, because of a server shutdown
				// switch state back to pending
				await _builds.UpdateAsync(build, u => u
					.Set(b => b.Message, null)
					.Set(b => b.PercentCompleted, 0)
					.Set(b => b.State, BuildState.Pending), cancellationToken: CancellationToken.None);
			}

			throw;
		}
		catch (Exception e)
		{
			await using (await rwLock.WriterLockAsync(cancellationToken: cancellationToken))
			{
				engine = (await _engines.UpdateAsync(engine, u => u.Set(e => e.IsBuilding, false),
					cancellationToken: CancellationToken.None))!;
			}


			build = (await _builds.UpdateAsync(buildId, u => u
				.Set(b => b.State, BuildState.Faulted)
				.Set(b => b.Message, e.Message)
				.Set(b => b.DateFinished, DateTime.UtcNow), cancellationToken: CancellationToken.None))!;
			_logger.LogError(0, e, "Build faulted ({0})", engineId);
			await _webhookService.TriggerEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);
			throw;
		}
		finally
		{
			smtModelTrainer?.Dispose();
			truecaseTrainer?.Dispose();
		}
	}
}
