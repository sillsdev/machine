namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class NmtEngineServiceTests
{
    [Test]
    public async Task StartBuildAsync()
    {
        using var env = new TestEnvironment();
        TranslationEngine engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildRevision, Is.EqualTo(1));
        await env.Service.StartBuildAsync("engine1", "build1", "{}", Array.Empty<Corpus>());
        await env.WaitForBuildToFinishAsync();
        engine = env.Engines.Get("engine1");
        Assert.That(engine.CurrentBuild, Is.Null);
        Assert.That(engine.BuildRevision, Is.EqualTo(2));
    }

    [Test]
    public async Task CancelBuildAsync_Building()
    {
        using var env = new TestEnvironment();

        var cts = new CancellationTokenSource();
        env.ClearMLService.When(x => x.StopTaskAsync("job1", Arg.Any<CancellationToken>())).Do(_ => cts.Cancel());
        env.TrainJobFunc = async () =>
        {
            await env.BuildJobService.BuildJobStartedAsync("engine1", "build1");

            while (!cts.IsCancellationRequested)
                await Task.Delay(50);

            await env.BuildJobService.BuildJobFinishedAsync("engine1", "build1", buildComplete: false);
        };

        TranslationEngine engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildRevision, Is.EqualTo(1));
        await env.Service.StartBuildAsync("engine1", "build1", "{}", Array.Empty<Corpus>());
        await env.WaitForBuildToStartAsync();
        engine = env.Engines.Get("engine1");
        Assert.That(engine.CurrentBuild, Is.Not.Null);
        Assert.That(engine.CurrentBuild.JobState, Is.EqualTo(BuildJobState.Active));
        await env.Service.CancelBuildAsync("engine1");
        await env.WaitForBuildToFinishAsync();
        engine = env.Engines.Get("engine1");
        Assert.That(engine.CurrentBuild, Is.Null);
        Assert.That(engine.BuildRevision, Is.EqualTo(1));
    }

    [Test]
    public void CancelBuildAsync_NotBuilding()
    {
        using var env = new TestEnvironment();
        Assert.ThrowsAsync<InvalidOperationException>(() => env.Service.CancelBuildAsync("engine1"));
    }

    [Test]
    public async Task DeleteAsync_WhileBuilding()
    {
        using var env = new TestEnvironment();

        var cts = new CancellationTokenSource();
        env.ClearMLService.When(x => x.StopTaskAsync("job1", Arg.Any<CancellationToken>())).Do(_ => cts.Cancel());
        env.TrainJobFunc = async () =>
        {
            await env.BuildJobService.BuildJobStartedAsync("engine1", "build1");

            while (!cts.IsCancellationRequested)
                await Task.Delay(50);

            await env.BuildJobService.BuildJobFinishedAsync("engine1", "build1", buildComplete: false);
        };

        TranslationEngine engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildRevision, Is.EqualTo(1));
        await env.Service.StartBuildAsync("engine1", "build1", "{}", Array.Empty<Corpus>());
        await env.WaitForBuildToStartAsync();
        engine = env.Engines.Get("engine1");
        Assert.That(engine.CurrentBuild, Is.Not.Null);
        Assert.That(engine.CurrentBuild.JobState, Is.EqualTo(BuildJobState.Active));
        await env.Service.DeleteAsync("engine1");
        // ensure that the train job has completed
        if (env.TrainJobTask is not null)
            await env.TrainJobTask;
        Assert.That(env.Engines.Contains("engine1"), Is.False);
    }

    private class TestEnvironment : DisposableBase
    {
        private readonly Hangfire.InMemory.InMemoryStorage _memoryStorage;
        private readonly BackgroundJobClient _jobClient;
        private BackgroundJobServer _jobServer;
        private readonly IDistributedReaderWriterLockFactory _lockFactory;

        public TestEnvironment()
        {
            if (!Sldr.IsInitialized)
                Sldr.Initialize(offlineMode: true);

            TrainJobFunc = RunMockTrainJob;
            Engines = new MemoryRepository<TranslationEngine>();
            Engines.Add(
                new TranslationEngine
                {
                    Id = "engine1",
                    EngineId = "engine1",
                    SourceLanguage = "es",
                    TargetLanguage = "en",
                    BuildRevision = 1
                }
            );
            _memoryStorage = new Hangfire.InMemory.InMemoryStorage();
            _jobClient = new BackgroundJobClient(_memoryStorage);
            PlatformService = Substitute.For<IPlatformService>();
            _lockFactory = new DistributedReaderWriterLockFactory(
                new OptionsWrapper<ServiceOptions>(new ServiceOptions { ServiceId = "host" }),
                new MemoryRepository<RWLock>(),
                new ObjectIdGenerator()
            );
            ClearMLService = Substitute.For<IClearMLService>();
            ClearMLService
                .GetProjectIdAsync("engine1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<string?>("project1"));
            ClearMLService
                .CreateTaskAsync("build1", "project1", Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("job1"));
            ClearMLService
                .When(x => x.EnqueueTaskAsync("job1", Arg.Any<CancellationToken>()))
                .Do(_ => TrainJobTask = Task.Run(TrainJobFunc));
            SharedFileService = new SharedFileService(Substitute.For<ILoggerFactory>());
            var clearMLOptions = Substitute.For<IOptionsMonitor<ClearMLOptions>>();
            clearMLOptions.CurrentValue.Returns(new ClearMLOptions());
            BuildJobService = new BuildJobService(
                new IBuildJobRunner[]
                {
                    new HangfireBuildJobRunner(_jobClient, new[] { new NmtHangfireBuildJobFactory() }),
                    new ClearMLBuildJobRunner(
                        ClearMLService,
                        new[]
                        {
                            new NmtClearMLBuildJobFactory(
                                SharedFileService,
                                Substitute.For<ILanguageTagService>(),
                                Engines,
                                clearMLOptions
                            )
                        }
                    )
                },
                Engines,
                new OptionsWrapper<BuildJobOptions>(new BuildJobOptions())
            );
            var clearMLOptionsMonitor = Substitute.For<IOptions<ClearMLOptions>>();
            clearMLOptionsMonitor.Value.Returns(new ClearMLOptions());
            ClearMLMonitorService = new ClearMLMonitorService(
                Substitute.For<IServiceProvider>(),
                ClearMLService,
                SharedFileService,
                clearMLOptionsMonitor,
                Substitute.For<ILogger<ClearMLMonitorService>>()
            );
            _jobServer = CreateJobServer();
            Service = CreateService();
        }

        public NmtEngineService Service { get; private set; }
        public ClearMLMonitorService ClearMLMonitorService { get; }
        public MemoryRepository<TranslationEngine> Engines { get; }
        public IPlatformService PlatformService { get; }
        public IClearMLService ClearMLService { get; }
        public ISharedFileService SharedFileService { get; }
        public IBuildJobService BuildJobService { get; }
        public Func<Task> TrainJobFunc { get; set; }
        public Task? TrainJobTask { get; private set; }

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
                CancellationCheckInterval = TimeSpan.FromMilliseconds(50),
            };
            return new BackgroundJobServer(jobServerOptions, _memoryStorage);
        }

        private NmtEngineService CreateService()
        {
            return new NmtEngineService(
                PlatformService,
                _lockFactory,
                new MemoryDataAccessContext(),
                Engines,
                BuildJobService,
                new LanguageTagService(),
                ClearMLMonitorService
            );
        }

        public Task WaitForBuildToFinishAsync()
        {
            return WaitForBuildState(e => e.CurrentBuild is null);
        }

        public Task WaitForBuildToStartAsync()
        {
            return WaitForBuildState(e =>
                e.CurrentBuild!.JobState is BuildJobState.Active && e.CurrentBuild!.Stage == NmtBuildStages.Train
            );
        }

        private async Task WaitForBuildState(Func<TranslationEngine, bool> predicate)
        {
            using ISubscription<TranslationEngine> subscription = await Engines.SubscribeAsync(e =>
                e.EngineId == "engine1"
            );
            while (true)
            {
                TranslationEngine? engine = subscription.Change.Entity;
                if (engine is not null && predicate(engine))
                    break;
                await subscription.WaitForChangeAsync();
            }
        }

        private async Task RunMockTrainJob()
        {
            await BuildJobService.BuildJobStartedAsync("engine1", "build1");

            await using (var stream = await SharedFileService.OpenWriteAsync("builds/build1/pretranslate.trg.json"))
            {
                await JsonSerializer.SerializeAsync(stream, Array.Empty<Pretranslation>());
            }

            await BuildJobService.StartBuildJobAsync(
                BuildJobType.Cpu,
                TranslationEngineType.Nmt,
                "engine1",
                "build1",
                NmtBuildStages.Postprocess,
                (0, 0.0)
            );
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
                if (jobType == typeof(NmtPreprocessBuildJob))
                {
                    return new NmtPreprocessBuildJob(
                        _env.PlatformService,
                        _env.Engines,
                        _env._lockFactory,
                        Substitute.For<ILogger<NmtPreprocessBuildJob>>(),
                        _env.BuildJobService,
                        _env.SharedFileService,
                        Substitute.For<ICorpusService>(),
                        new LanguageTagService()
                    );
                }
                if (jobType == typeof(NmtPostprocessBuildJob))
                {
                    return new NmtPostprocessBuildJob(
                        _env.PlatformService,
                        _env.Engines,
                        _env._lockFactory,
                        _env.BuildJobService,
                        Substitute.For<ILogger<NmtPostprocessBuildJob>>(),
                        _env.SharedFileService
                    );
                }
                return base.ActivateJob(jobType);
            }
        }
    }
}
