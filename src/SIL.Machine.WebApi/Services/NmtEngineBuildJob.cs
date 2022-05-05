namespace SIL.Machine.WebApi.Services;

public class NmtEngineBuildJob
{
	private readonly IRepository<TranslationEngine> _engines;
	private readonly IRepository<Build> _builds;
	private readonly IDistributedReaderWriterLockFactory _lockFactory;
	private readonly ILogger<NmtEngineBuildJob> _logger;
	private readonly IWebhookService _webhookService;
	private readonly INmtBuildJobRunner _jobRunner;

	public NmtEngineBuildJob(IRepository<TranslationEngine> engines, IRepository<Build> builds,
		IDistributedReaderWriterLockFactory lockFactory, ILogger<NmtEngineBuildJob> logger,
		IWebhookService webhookService, INmtBuildJobRunner jobRunner)
	{
		_engines = engines;
		_builds = builds;
		_lockFactory = lockFactory;
		_logger = logger;
		_webhookService = webhookService;
		_jobRunner = jobRunner;
	}

	[AutomaticRetry(Attempts = 0)]
	public async Task RunAsync(string engineId, string buildId, PerformContext performContext,
		CancellationToken cancellationToken)
	{
		IDistributedReaderWriterLock rwLock = _lockFactory.Create(engineId);
		try
		{
			Build? build;
			TranslationEngine? engine;
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
			}

			await _jobRunner.RunAsync(engineId, buildId, cancellationToken);

			await using (await rwLock.WriterLockAsync(cancellationToken: cancellationToken))
			{
				engine = await _engines.UpdateAsync(engineId, u => u
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
			TranslationEngine? engine;
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
					.Set(b => b.Step, 0)
					.Set(b => b.PercentCompleted, 0)
					.Set(b => b.State, BuildState.Pending), cancellationToken: CancellationToken.None);
			}

			throw;
		}
		catch (Exception e)
		{
			TranslationEngine? engine;
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
	}
}
