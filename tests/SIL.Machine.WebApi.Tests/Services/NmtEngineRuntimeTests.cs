namespace SIL.Machine.WebApi.Services;

[TestFixture]
public class NmtEngineRuntimeTests
{
	[Test]
	public async Task CancelBuildAsync()
	{
		using var env = new TestEnvironment();
		await env.JobRunner.RunAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Do<CancellationToken>(ct =>
		{
			while (true)
				ct.ThrowIfCancellationRequested();
		}));
		await env.Runtime.InitNewAsync();
		Build build = await env.Runtime.StartBuildAsync();
		Assert.That(build, Is.Not.Null);
		await env.WaitForBuildToStartAsync(build.Id);
		build = env.Builds.Get(build.Id);
		Assert.That(build.State, Is.EqualTo(BuildState.Active));
		await env.Runtime.CancelBuildAsync();
		await env.WaitForBuildToFinishAsync(build.Id);
		build = env.Builds.Get(build.Id);
		Assert.That(build.State, Is.EqualTo(BuildState.Canceled));
	}

	private class TestEnvironment : DisposableBase
	{
		private readonly MemoryStorage _memoryStorage;
		private readonly BackgroundJobClient _jobClient;
		private BackgroundJobServer _jobServer;
		private readonly IDistributedReaderWriterLockFactory _lockFactory;

		public TestEnvironment()
		{
			Engines = new MemoryRepository<TranslationEngine>();
			Engines.Add(new TranslationEngine
			{
				Id = "engine1",
				Owner = "client",
				SourceLanguageTag = "es",
				TargetLanguageTag = "en",
				Type = TranslationEngineType.Nmt
			});
			Builds = new MemoryRepository<Build>();
			EngineOptions = new TranslationEngineOptions();
			_memoryStorage = new MemoryStorage();
			_jobClient = new BackgroundJobClient(_memoryStorage);
			WebhookService = Substitute.For<IWebhookService>();
			JobRunner = Substitute.For<INmtBuildJobRunner>();
			_lockFactory = new DistributedReaderWriterLockFactory(
				new OptionsWrapper<ServiceOptions>(new ServiceOptions { ServiceId = "host" }),
				new MemoryRepository<RWLock>());
			_jobServer = CreateJobServer();
			Runtime = CreateRuntime();
		}

		public NmtEngineRuntime Runtime { get; private set; }
		public MemoryRepository<TranslationEngine> Engines { get; }
		public MemoryRepository<Build> Builds { get; }
		public TranslationEngineOptions EngineOptions { get; }
		public IWebhookService WebhookService { get; }
		public INmtBuildJobRunner JobRunner { get; }

		public void StopServer()
		{
			Runtime.Dispose();
			_jobServer.Dispose();
		}

		public void StartServer()
		{
			_jobServer = CreateJobServer();
			Runtime = CreateRuntime();
		}

		private BackgroundJobServer CreateJobServer()
		{
			var jobServerOptions = new BackgroundJobServerOptions
			{
				Activator = new EnvActivator(this)
			};
			return new BackgroundJobServer(jobServerOptions, _memoryStorage);
		}

		private NmtEngineRuntime CreateRuntime()
		{
			return new NmtEngineRuntime(Builds, _jobClient, _lockFactory, "engine1");
		}

		public Task WaitForBuildToFinishAsync(string buildId)
		{
			return WaitForBuildState(buildId, b => b.DateFinished is not null);
		}

		public Task WaitForBuildToStartAsync(string buildId)
		{
			return WaitForBuildState(buildId, b => b.State != BuildState.Pending);
		}

		private async Task WaitForBuildState(string buildId, Func<Build, bool> predicate)
		{
			using ISubscription<Build> subscription = await Builds.SubscribeAsync(b => b.Id == buildId);
			while (true)
			{
				Build? build = subscription.Change.Entity;
				if (build != null && predicate(build))
					break;
				await subscription.WaitForChangeAsync();
			}
		}

		protected override void DisposeManagedResources()
		{
			Runtime.Dispose();
			_jobServer.Dispose();
		}

		private class EnvActivator : JobActivator
		{
			private readonly TestEnvironment _env;

			public EnvActivator(TestEnvironment env)
			{
				_env = env;
			}

			public override object ActivateJob(Type jobType)
			{
				if (jobType == typeof(NmtEngineBuildJob))
				{
					return new NmtEngineBuildJob(_env.Engines, _env.Builds, _env._lockFactory,
						Substitute.For<ILogger<NmtEngineBuildJob>>(), _env.WebhookService, _env.JobRunner);
				}
				return base.ActivateJob(jobType);
			}
		}
	}
}
