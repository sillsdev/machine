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
        Assert.Multiple(() =>
        {
            Assert.That(engine.CurrentBuild, Is.Null);
            Assert.That(engine.BuildRevision, Is.EqualTo(2));
            Assert.That(engine.IsModelPersisted, Is.False);
        });
    }

    [Test]
    public async Task CancelBuildAsync_Building()
    {
        using var env = new TestEnvironment();
        env.UseInfiniteTrainJob();

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
        env.UseInfiniteTrainJob();

        TranslationEngine engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildRevision, Is.EqualTo(1));
        await env.Service.StartBuildAsync("engine1", "build1", "{}", Array.Empty<Corpus>());
        await env.WaitForBuildToStartAsync();
        engine = env.Engines.Get("engine1");
        Assert.That(engine.CurrentBuild, Is.Not.Null);
        Assert.That(engine.CurrentBuild.JobState, Is.EqualTo(BuildJobState.Active));
        await env.Service.DeleteAsync("engine1");
        // ensure that the train job has completed
        await env.WaitForBuildToFinishAsync();
        Assert.That(env.Engines.Contains("engine1"), Is.False);
    }

    private class TestEnvironment : ObjectModel.DisposableBase
    {
        private readonly Hangfire.InMemory.InMemoryStorage _memoryStorage;
        private readonly BackgroundJobClient _jobClient;
        private BackgroundJobServer _jobServer;
        private readonly IDistributedReaderWriterLockFactory _lockFactory;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private Func<Task> _trainJobFunc;
        private Task? _trainJobTask;

        public TestEnvironment()
        {
            if (!Sldr.IsInitialized)
                Sldr.Initialize(offlineMode: true);

            _trainJobFunc = RunNormalTrainJob;
            Engines = new MemoryRepository<TranslationEngine>();
            Engines.Add(
                new TranslationEngine
                {
                    Id = "engine1",
                    EngineId = "engine1",
                    Type = TranslationEngineType.Nmt,
                    SourceLanguage = "es",
                    TargetLanguage = "en",
                    BuildRevision = 1,
                    IsModelPersisted = false
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
                .CreateTaskAsync(
                    "build1",
                    "project1",
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns(Task.FromResult("job1"));
            ClearMLService
                .When(x => x.EnqueueTaskAsync("job1", Arg.Any<string>(), Arg.Any<CancellationToken>()))
                .Do(_ => _trainJobTask = Task.Run(_trainJobFunc));
            ClearMLService
                .When(x => x.StopTaskAsync("job1", Arg.Any<CancellationToken>()))
                .Do(_ => _cancellationTokenSource.Cancel());
            SharedFileService = new SharedFileService(Substitute.For<ILoggerFactory>());
            var buildJobOptions = Substitute.For<IOptionsMonitor<BuildJobOptions>>();
            buildJobOptions.CurrentValue.Returns(
                new BuildJobOptions
                {
                    ClearML =
                    [
                        new ClearMLBuildQueue()
                        {
                            TranslationEngineType = TranslationEngineType.Nmt,
                            ModelType = "huggingface",
                            DockerImage = "default",
                            Queue = "default"
                        },
                        new ClearMLBuildQueue()
                        {
                            TranslationEngineType = TranslationEngineType.SmtTransfer,
                            ModelType = "thot",
                            DockerImage = "default",
                            Queue = "default"
                        }
                    ]
                }
            );
            BuildJobService = new BuildJobService(
                [
                    new HangfireBuildJobRunner(_jobClient, [new NmtHangfireBuildJobFactory()]),
                    new ClearMLBuildJobRunner(
                        ClearMLService,
                        [
                            new NmtClearMLBuildJobFactory(
                                SharedFileService,
                                Substitute.For<ILanguageTagService>(),
                                Engines
                            )
                        ],
                        buildJobOptions
                    )
                ],
                Engines
            );
            var clearMLOptions = Substitute.For<IOptionsMonitor<ClearMLOptions>>();
            clearMLOptions.CurrentValue.Returns(new ClearMLOptions());
            ClearMLQueueService = new ClearMLMonitorService(
                Substitute.For<IServiceProvider>(),
                ClearMLService,
                SharedFileService,
                clearMLOptions,
                buildJobOptions,
                Substitute.For<ILogger<ClearMLMonitorService>>()
            );
            _jobServer = CreateJobServer();
            Service = CreateService();
        }

        public NmtEngineService Service { get; private set; }
        public IClearMLQueueService ClearMLQueueService { get; }
        public MemoryRepository<TranslationEngine> Engines { get; }
        public IPlatformService PlatformService { get; }
        public IClearMLService ClearMLService { get; }
        public ISharedFileService SharedFileService { get; }
        public IBuildJobService BuildJobService { get; }

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
                ClearMLQueueService,
                SharedFileService
            );
        }

        public async Task WaitForBuildToFinishAsync()
        {
            await WaitForBuildState(e => e.CurrentBuild is null);
            if (_trainJobTask is not null)
                await _trainJobTask;
        }

        public Task WaitForBuildToStartAsync()
        {
            return WaitForBuildState(e =>
                e.CurrentBuild!.JobState is BuildJobState.Active && e.CurrentBuild!.Stage == BuildStage.Train
            );
        }

        public void UseInfiniteTrainJob()
        {
            _trainJobFunc = RunInfiniteTrainJob;
        }

        private async Task WaitForBuildState(Func<TranslationEngine, bool> predicate)
        {
            using ISubscription<TranslationEngine> subscription = await Engines.SubscribeAsync(e =>
                e.EngineId == "engine1"
            );
            while (true)
            {
                TranslationEngine? engine = subscription.Change.Entity;
                if (engine is null || predicate(engine))
                    break;
                await subscription.WaitForChangeAsync();
            }
        }

        private async Task RunNormalTrainJob()
        {
            await BuildJobService.BuildJobStartedAsync("engine1", "build1");

            await using Stream stream = await SharedFileService.OpenWriteAsync("builds/build1/pretranslate.trg.json");

            await BuildJobService.StartBuildJobAsync(
                BuildJobRunnerType.Hangfire,
                "engine1",
                "build1",
                BuildStage.Postprocess,
                (0, 0.0)
            );
        }

        private async Task RunInfiniteTrainJob()
        {
            await BuildJobService.BuildJobStartedAsync("engine1", "build1");

            while (!_cancellationTokenSource.IsCancellationRequested)
                await Task.Delay(50);

            await BuildJobService.BuildJobFinishedAsync("engine1", "build1", buildComplete: false);
        }

        protected override void DisposeManagedResources()
        {
            _jobServer.Dispose();
            _cancellationTokenSource.Dispose();
        }

        private class EnvActivator(TestEnvironment env) : JobActivator
        {
            private readonly TestEnvironment _env = env;

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
                if (jobType == typeof(PostprocessBuildJob))
                {
                    return new PostprocessBuildJob(
                        _env.PlatformService,
                        _env.Engines,
                        _env._lockFactory,
                        _env.BuildJobService,
                        Substitute.For<ILogger<PostprocessBuildJob>>(),
                        _env.SharedFileService
                    );
                }
                return base.ActivateJob(jobType);
            }
        }
    }
}
