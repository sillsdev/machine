namespace SIL.Machine.WebApi.Services;

[TestFixture]
public class ClearMLNmtEngineRuntimeTests
{
    [Test]
    public async Task CancelBuildAsync()
    {
        using var env = new TestEnvironment();
        env.ClearMLService
            .CreateTaskAsync(Arg.Any<string>(), "project1", "es", "en", Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("task1"));
        var task = new ClearMLTask
        {
            Id = "task1",
            Project = new ClearMLProject { Id = "project1" },
            Status = ClearMLTaskStatus.InProgress
        };
        bool first = true;
        env.ClearMLService
            .GetTaskAsync(Arg.Any<string>(), "project1", Arg.Any<CancellationToken>())
            .Returns(
                x =>
                {
                    if (first)
                    {
                        first = false;
                        return Task.FromResult<ClearMLTask?>(null);
                    }
                    return Task.FromResult<ClearMLTask?>(task);
                }
            );
        env.ClearMLService
            .GetTaskAsync("task1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ClearMLTask?>(task));
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
        await env.ClearMLService.Received().StopTaskAsync("task1", Arg.Any<CancellationToken>());
    }

    private class TestEnvironment : DisposableBase
    {
        private readonly MemoryStorage _memoryStorage;
        private readonly BackgroundJobClient _jobClient;
        private BackgroundJobServer _jobServer;
        private readonly IDistributedReaderWriterLockFactory _lockFactory;
        private readonly ICorpusService _corpusService;
        private readonly ISharedFileService _sharedFileService;
        private readonly IOptionsMonitor<ClearMLOptions> _options;

        public TestEnvironment()
        {
            Engines = new MemoryRepository<TranslationEngine>();
            Engines.Add(
                new TranslationEngine
                {
                    Id = "engine1",
                    Owner = "client",
                    SourceLanguageTag = "es",
                    TargetLanguageTag = "en",
                    Type = TranslationEngineType.Nmt
                }
            );
            Builds = new MemoryRepository<Build>();
            Pretranslations = new MemoryRepository<Pretranslation>();
            EngineOptions = new TranslationEngineOptions();
            _memoryStorage = new MemoryStorage();
            _jobClient = new BackgroundJobClient(_memoryStorage);
            WebhookService = Substitute.For<IWebhookService>();
            ClearMLService = Substitute.For<IClearMLService>();
            ClearMLService
                .GetProjectIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<string?>("project1"));
            _lockFactory = new DistributedReaderWriterLockFactory(
                new OptionsWrapper<ServiceOptions>(new ServiceOptions { ServiceId = "host" }),
                new MemoryRepository<RWLock>()
            );
            _corpusService = Substitute.For<ICorpusService>();
            _sharedFileService = new SharedFileService();
            _options = Substitute.For<IOptionsMonitor<ClearMLOptions>>();
            _options.CurrentValue.Returns(new ClearMLOptions { BuildPollingTimeout = TimeSpan.FromMilliseconds(50) });
            _jobServer = CreateJobServer();
            Runtime = CreateRuntime();
        }

        public ClearMLNmtEngineRuntime Runtime { get; private set; }
        public MemoryRepository<TranslationEngine> Engines { get; }
        public MemoryRepository<Build> Builds { get; }
        public MemoryRepository<Pretranslation> Pretranslations { get; }
        public TranslationEngineOptions EngineOptions { get; }
        public IWebhookService WebhookService { get; }
        public IClearMLService ClearMLService { get; }

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
                Activator = new EnvActivator(this),
                Queues = new[] { "nmt" },
                CancellationCheckInterval = TimeSpan.FromMilliseconds(100),
            };
            return new BackgroundJobServer(jobServerOptions, _memoryStorage);
        }

        private ClearMLNmtEngineRuntime CreateRuntime()
        {
            return new ClearMLNmtEngineRuntime(Engines, Builds, ClearMLService, _jobClient, _lockFactory, "engine1");
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
                if (jobType == typeof(ClearMLNmtEngineBuildJob))
                {
                    return new ClearMLNmtEngineBuildJob(
                        _env.Engines,
                        _env.Builds,
                        _env.Pretranslations,
                        Substitute.For<ILogger<ClearMLNmtEngineBuildJob>>(),
                        _env.WebhookService,
                        _env.ClearMLService,
                        _env._corpusService,
                        _env._sharedFileService,
                        _env._options
                    );
                }
                return base.ActivateJob(jobType);
            }
        }
    }
}
