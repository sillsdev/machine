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
		IDistributedReaderWriterLock rwLock = _lockFactory.Create(engineId);

		ITrainer? smtModelTrainer = null;
		ITrainer? truecaseTrainer = null;
		try
		{
			Build? build;
			Engine? engine;
			var stopwatch = new Stopwatch();
			await using (await rwLock.WriterLockAsync(cancellationToken: cancellationToken))
			{
				build = await _builds.UpdateAsync(b => b.Id == buildId && b.State == BuildState.Pending, u => u
					.Set(b => b.State, BuildState.Active)
					.Set(b => b.JobId, performContext.BackgroundJob.Id), cancellationToken: cancellationToken);
				if (build is null)
					throw new OperationCanceledException();

				engine = await _engines.UpdateAsync(engineId, u => u.Set(e => e.IsBuilding, true),
					cancellationToken: CancellationToken.None);
				if (engine is null)
					return;

				await _webhookService.SendEventAsync(WebhookEvent.BuildStarted, engine.Owner, build);

				_logger.LogInformation("Build started ({0})", buildId);
				stopwatch.Start();

				await _trainSegmentPairs.DeleteAllAsync(p => p.EngineRef == engineId, cancellationToken);

				var tokenizer = new LatinWordTokenizer();
				IReadOnlyDictionary<string, ITextCorpus> sourceCorpora = await _dataFileService.CreateTextCorporaAsync(
					engine.Id, CorpusType.Source);
				IReadOnlyDictionary<string, ITextCorpus> targetCorpora = await _dataFileService.CreateTextCorporaAsync(
					engine.Id, CorpusType.Target);

				IEnumerable<ParallelTextRow> parallelCorpus = CreateParallelCorpus(sourceCorpora, targetCorpora)
					.Tokenize(tokenizer)
					.Lowercase();

				IEnumerable<TextRow> targetCorpus = targetCorpora.Values.SelectMany(c => c)
					.Tokenize(tokenizer);

				smtModelTrainer = _smtModelFactory.CreateTrainer(engineId, parallelCorpus);
				truecaseTrainer = _truecaserFactory.CreateTrainer(engineId, targetCorpus);
			}

			var progress = new BuildProgress(_builds, buildId);
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
						smtEngine.TrainSegment(segmentPair.Source.Lowercase(), segmentPair.Target.Lowercase());
						truecaser.TrainSegment(segmentPair.Target, segmentPair.SentenceStart);
					}
				}

				engine = await _engines.UpdateAsync(engineId, u => u
					.Set(e => e.Confidence,
						Math.Round(smtModelTrainer.Stats.Metrics["bleu"], 4, MidpointRounding.AwayFromZero))
					.Set(e => e.TrainedSegmentCount, smtModelTrainer.Stats.TrainedSegmentCount + segmentPairs.Count)
					.Set(e => e.IsBuilding, false)
					.Inc(e => e.ModelRevision),
					cancellationToken: CancellationToken.None);
				if (engine is null)
					return;
			}

			build = await _builds.UpdateAsync(buildId, u => u
				.Set(b => b.State, BuildState.Completed)
				.Set(b => b.Message, "Completed")
				.Set(b => b.DateFinished, DateTime.UtcNow), cancellationToken: CancellationToken.None);
			if (build is null)
				return;

			stopwatch.Stop();
			_logger.LogInformation("Build completed in {0}ms ({1})", stopwatch.Elapsed.TotalMilliseconds, buildId);
			await _webhookService.SendEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);
		}
		catch (OperationCanceledException)
		{
			Engine? engine;
			await using (await rwLock.WriterLockAsync(cancellationToken: CancellationToken.None))
			{
				engine = await _engines.UpdateAsync(engineId, u => u.Set(e => e.IsBuilding, false),
					cancellationToken: CancellationToken.None);
				if (engine is null)
					return;
			}

			Build? build = await _builds.UpdateAsync(b => b.Id == buildId && b.State == BuildState.Canceled, u => u
				.Set(b => b.DateFinished, DateTime.UtcNow), cancellationToken: CancellationToken.None);
			if (build is not null)
			{
				_logger.LogInformation("Build canceled ({0})", buildId);
				await _webhookService.SendEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);
			}
			else
			{
				// the build was canceled, because of a server shutdown
				// switch state back to pending
				await _builds.UpdateAsync(buildId, u => u
					.Set(b => b.Message, "Canceled")
					.Set(b => b.PercentCompleted, 0)
					.Set(b => b.State, BuildState.Pending), cancellationToken: CancellationToken.None);
			}

			throw;
		}
		catch (Exception e)
		{
			Engine? engine;
			await using (await rwLock.WriterLockAsync(cancellationToken: CancellationToken.None))
			{
				engine = await _engines.UpdateAsync(engineId, u => u.Set(e => e.IsBuilding, false),
					cancellationToken: CancellationToken.None);
				if (engine is null)
					return;
			}

			Build? build = await _builds.UpdateAsync(buildId, u => u
				.Set(b => b.State, BuildState.Faulted)
				.Set(b => b.Message, e.Message)
				.Set(b => b.DateFinished, DateTime.UtcNow), cancellationToken: CancellationToken.None);
			if (build is null)
				return;
			_logger.LogError(0, e, "Build faulted ({0})", buildId);
			await _webhookService.SendEventAsync(WebhookEvent.BuildFinished, engine.Owner, build);
			throw;
		}
		finally
		{
			smtModelTrainer?.Dispose();
			truecaseTrainer?.Dispose();
		}
	}

	private static IEnumerable<ParallelTextRow> CreateParallelCorpus(
		IReadOnlyDictionary<string, ITextCorpus> sourceCorpora, IReadOnlyDictionary<string, ITextCorpus> targetCorpora)
	{
		var parallelCorpus = Enumerable.Empty<ParallelTextRow>();
		foreach (KeyValuePair<string, ITextCorpus> kvp in sourceCorpora)
		{
			if (targetCorpora.TryGetValue(kvp.Key, out ITextCorpus? targetCorpus))
			{
				ITextCorpus sourceCorpus = kvp.Value;
				parallelCorpus = parallelCorpus.Concat(sourceCorpus.AlignRows(targetCorpus));
			}
		}

		return parallelCorpus;
	}
}
