namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class ClearMLNmtEngineServiceTests
{
    [Test]
    public async Task CancelBuildAsync()
    {
        using var env = new TestEnvironment();
        env.ClearMLService
            .CreateTaskAsync(
                Arg.Any<string>(),
                "project1",
                "engine1",
                "es",
                "en",
                "memory:///",
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult("task1"));
        var task = new ClearMLTask
        {
            Id = "task1",
            Project = new ClearMLProject { Id = "project1" },
            Status = ClearMLTaskStatus.InProgress
        };
        bool first = true;
        env.ClearMLService
            .GetTaskByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                if (first)
                {
                    first = false;
                    return Task.FromResult<ClearMLTask?>(null);
                }
                return Task.FromResult<ClearMLTask?>(task);
            });
        env.ClearMLService
            .GetTaskByIdAsync("task1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ClearMLTask?>(task));
        await env.Service.StartBuildAsync("engine1", "build1", Array.Empty<Corpus>());
        await env.WaitForBuildToStartAsync();
        TranslationEngine engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildState, Is.EqualTo(BuildState.Active));
        await env.Service.CancelBuildAsync("engine1");
        await env.WaitForBuildToFinishAsync();
        engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildState, Is.EqualTo(BuildState.None));
        await env.ClearMLService.Received().StopTaskAsync("task1", Arg.Any<CancellationToken>());
    }

    private class TestEnvironment : DisposableBase
    {
        private readonly MemoryStorage _memoryStorage;
        private readonly BackgroundJobClient _jobClient;
        private BackgroundJobServer _jobServer;
        private readonly IDistributedReaderWriterLockFactory _lockFactory;
        private readonly ISharedFileService _sharedFileService;
        private readonly IOptionsMonitor<ClearMLNmtEngineOptions> _options;

        public TestEnvironment()
        {
            Engines = new MemoryRepository<TranslationEngine>();
            Engines.Add(
                new TranslationEngine
                {
                    Id = "engine1",
                    EngineId = "engine1",
                    SourceLanguage = "es",
                    TargetLanguage = "en"
                }
            );
            EngineOptions = new SmtTransferEngineOptions();
            _memoryStorage = new MemoryStorage();
            _jobClient = new BackgroundJobClient(_memoryStorage);
            PlatformService = Substitute.For<IPlatformService>();
            ClearMLService = Substitute.For<IClearMLService>();
            ClearMLService
                .GetProjectIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<string?>("project1"));
            _lockFactory = new DistributedReaderWriterLockFactory(
                new OptionsWrapper<ServiceOptions>(new ServiceOptions { ServiceId = "host" }),
                new MemoryRepository<RWLock>(),
                new ObjectIdGenerator()
            );
            _sharedFileService = new SharedFileService(Substitute.For<ILoggerFactory>());
            _options = Substitute.For<IOptionsMonitor<ClearMLNmtEngineOptions>>();
            _options.CurrentValue.Returns(
                new ClearMLNmtEngineOptions { BuildPollingTimeout = TimeSpan.FromMilliseconds(50) }
            );
            _jobServer = CreateJobServer();
            Service = CreateService();
        }

        public ClearMLNmtEngineService Service { get; private set; }
        public MemoryRepository<TranslationEngine> Engines { get; }
        public SmtTransferEngineOptions EngineOptions { get; }
        public IPlatformService PlatformService { get; }
        public IClearMLService ClearMLService { get; }

        public void StopServer()
        {
            _jobServer.Dispose();
        }

        public void StartServer()
        {
            _jobServer = CreateJobServer();
            Service = CreateService();
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

        private ClearMLNmtEngineService CreateService()
        {
            return new ClearMLNmtEngineService(
                _jobClient,
                PlatformService,
                _lockFactory,
                new MemoryDataAccessContext(),
                Engines,
                ClearMLService
            );
        }

        public Task WaitForBuildToFinishAsync()
        {
            return WaitForBuildState(e => e.BuildState is BuildState.None);
        }

        public Task WaitForBuildToStartAsync()
        {
            return WaitForBuildState(e => e.BuildState is BuildState.Active);
        }

        private async Task WaitForBuildState(Func<TranslationEngine, bool> predicate)
        {
            using ISubscription<TranslationEngine> subscription = await Engines.SubscribeAsync(
                e => e.EngineId == "engine1"
            );
            while (true)
            {
                TranslationEngine? build = subscription.Change.Entity;
                if (build is not null && predicate(build))
                    break;
                await subscription.WaitForChangeAsync();
            }
        }

        protected override void DisposeManagedResources()
        {
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
                        _env.PlatformService,
                        _env.Engines,
                        Substitute.For<ILogger<ClearMLNmtEngineBuildJob>>(),
                        _env.ClearMLService,
                        _env._sharedFileService,
                        _env._options,
                        Substitute.For<ICorpusService>()
                    );
                }
                return base.ActivateJob(jobType);
            }
        }
    }
}
