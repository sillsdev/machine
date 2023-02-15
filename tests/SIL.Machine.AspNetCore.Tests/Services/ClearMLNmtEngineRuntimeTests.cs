namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class ClearMLNmtEngineRuntimeTests
{
    [Test]
    public async Task CancelBuildAsync()
    {
        using var env = new TestEnvironment();
        env.ClearMLService
            .CreateTaskAsync(Arg.Any<string>(), "project1", "engine1", "es", "en", Arg.Any<CancellationToken>())
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
        await env.Runtime.StartBuildAsync("build1");
        await env.WaitForBuildToStartAsync();
        TranslationEngine engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildState, Is.EqualTo(BuildState.Active));
        await env.Runtime.CancelBuildAsync();
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
        private readonly IOptionsMonitor<ClearMLOptions> _options;

        public TestEnvironment()
        {
            Engines = new MemoryRepository<TranslationEngine>();
            Engines.Add(new TranslationEngine { Id = "engine1", EngineId = "engine1" });
            EngineOptions = new TranslationEngineOptions();
            _memoryStorage = new MemoryStorage();
            _jobClient = new BackgroundJobClient(_memoryStorage);
            PlatformService = Substitute.For<IPlatformService>();
            PlatformService
                .GetTranslationEngineInfoAsync("engine1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new TranslationEngineInfo("nmt", "engine1", "Engine 1", "es", "en")));
            ClearMLService = Substitute.For<IClearMLService>();
            ClearMLService
                .GetProjectIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<string?>("project1"));
            _lockFactory = new DistributedReaderWriterLockFactory(
                new OptionsWrapper<ServiceOptions>(new ServiceOptions { ServiceId = "host" }),
                new MemoryRepository<RWLock>()
            );
            _sharedFileService = new SharedFileService();
            _options = Substitute.For<IOptionsMonitor<ClearMLOptions>>();
            _options.CurrentValue.Returns(new ClearMLOptions { BuildPollingTimeout = TimeSpan.FromMilliseconds(50) });
            _jobServer = CreateJobServer();
            Runtime = CreateRuntime();
        }

        public ClearMLNmtEngineRuntime Runtime { get; private set; }
        public MemoryRepository<TranslationEngine> Engines { get; }
        public TranslationEngineOptions EngineOptions { get; }
        public IPlatformService PlatformService { get; }
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
            return new ClearMLNmtEngineRuntime(
                PlatformService,
                ClearMLService,
                _jobClient,
                _lockFactory,
                Engines,
                "engine1"
            );
        }

        public Task WaitForBuildToFinishAsync()
        {
            return WaitForBuildState(e => e.BuildState is BuildState.None);
        }

        public Task WaitForBuildToStartAsync()
        {
            return WaitForBuildState(e => e.BuildState is not BuildState.Pending);
        }

        private async Task WaitForBuildState(Func<TranslationEngine, bool> predicate)
        {
            using ISubscription<TranslationEngine> subscription = await Engines.SubscribeAsync(
                e => e.EngineId == "engine1"
            );
            while (true)
            {
                TranslationEngine? build = subscription.Change.Entity;
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
                        _env.PlatformService,
                        _env.Engines,
                        Substitute.For<ILogger<ClearMLNmtEngineBuildJob>>(),
                        _env.ClearMLService,
                        _env._sharedFileService,
                        _env._options
                    );
                }
                return base.ActivateJob(jobType);
            }
        }
    }
}
