namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class SmtTransferEngineServiceTests
{
    const string EngineId1 = "engine1";
    const string EngineId2 = "engine2";
    const string BuildId1 = "build1";
    const string CorpusId1 = "corpus1";

    [Test]
    public async Task CreateAsync()
    {
        using var env = new TestEnvironment();
        await env.Service.CreateAsync(EngineId2, "Engine 2", "es", "en");
        TranslationEngine? engine = await env.Engines.GetAsync(e => e.EngineId == EngineId2);
        Assert.Multiple(() =>
        {
            Assert.That(engine, Is.Not.Null);
            Assert.That(engine?.EngineId, Is.EqualTo(EngineId2));
            Assert.That(engine?.BuildRevision, Is.EqualTo(0));
            Assert.That(engine?.IsModelPersisted, Is.True);
        });
        string engineDir = Path.Combine("translation_engines", EngineId2);
        _ = env.SmtModelFactory.Received().InitNewAsync(engineDir);
        _ = env.TransferEngineFactory.Received().InitNewAsync(engineDir);
    }

    [TestCase(BuildJobRunnerType.Hangfire)]
    [TestCase(BuildJobRunnerType.ClearML)]
    public async Task StartBuildAsync(BuildJobRunnerType trainJobRunnerType)
    {
        using var env = new TestEnvironment(trainJobRunnerType);
        TranslationEngine engine = env.Engines.Get(EngineId1);
        Assert.That(engine.BuildRevision, Is.EqualTo(1));
        // ensure that the SMT model was loaded before training
        await env.Service.TranslateAsync(EngineId1, n: 1, "esto es una prueba.");
        await env.Service.StartBuildAsync(
            EngineId1,
            BuildId1,
            null,
            [
                new Corpus()
                {
                    Id = CorpusId1,
                    SourceLanguage = "es",
                    TargetLanguage = "en",
                    SourceFiles = [],
                    TargetFiles = [],
                    TrainOnTextIds = null,
                    PretranslateTextIds = null
                }
            ]
        );
        await env.WaitForBuildToFinishAsync();
        _ = env
            .SmtBatchTrainer.Received()
            .TrainAsync(Arg.Any<IProgress<ProgressStatus>>(), Arg.Any<CancellationToken>());
        _ = env
            .TruecaserTrainer.Received()
            .TrainAsync(Arg.Any<IProgress<ProgressStatus>>(), Arg.Any<CancellationToken>());
        _ = env.SmtBatchTrainer.Received().SaveAsync(Arg.Any<CancellationToken>());
        _ = env.TruecaserTrainer.Received().SaveAsync(Arg.Any<CancellationToken>());
        engine = env.Engines.Get(EngineId1);
        Assert.That(engine.CurrentBuild, Is.Null);
        Assert.That(engine.BuildRevision, Is.EqualTo(2));
        // check if SMT model was reloaded upon first use after training
        env.SmtModel.ClearReceivedCalls();
        await env.Service.TranslateAsync(EngineId1, n: 1, "esto es una prueba.");
        env.SmtModel.Received().Dispose();
        _ = env.SmtModel.DidNotReceive().SaveAsync();
        _ = env.Truecaser.DidNotReceive().SaveAsync();
    }

    [TestCase(BuildJobRunnerType.Hangfire)]
    [TestCase(BuildJobRunnerType.ClearML)]
    public async Task CancelBuildAsync_Building(BuildJobRunnerType trainJobRunnerType)
    {
        using var env = new TestEnvironment(trainJobRunnerType);
        env.UseInfiniteTrainJob();

        await env.Service.StartBuildAsync(EngineId1, BuildId1, "{}", Array.Empty<Corpus>());
        await env.WaitForTrainingToStartAsync();
        TranslationEngine engine = env.Engines.Get(EngineId1);
        Assert.That(engine.CurrentBuild, Is.Not.Null);
        Assert.That(engine.CurrentBuild.JobState, Is.EqualTo(BuildJobState.Active));
        await env.Service.CancelBuildAsync(EngineId1);
        await env.WaitForBuildToFinishAsync();
        _ = env.SmtBatchTrainer.DidNotReceive().SaveAsync();
        _ = env.TruecaserTrainer.DidNotReceive().SaveAsync();
        engine = env.Engines.Get(EngineId1);
        Assert.That(engine.CurrentBuild, Is.Null);
    }

    [Test]
    public void CancelBuildAsync_NotBuilding()
    {
        using var env = new TestEnvironment();
        Assert.ThrowsAsync<InvalidOperationException>(() => env.Service.CancelBuildAsync(EngineId1));
    }

    [Test]
    public async Task StartBuildAsync_RestartUnfinishedBuild()
    {
        using var env = new TestEnvironment(BuildJobRunnerType.Hangfire);
        env.UseInfiniteTrainJob();

        await env.Service.StartBuildAsync(EngineId1, BuildId1, "{}", Array.Empty<Corpus>());
        await env.WaitForTrainingToStartAsync();
        TranslationEngine engine = env.Engines.Get(EngineId1);
        Assert.That(engine.CurrentBuild, Is.Not.Null);
        Assert.That(engine.CurrentBuild.JobState, Is.EqualTo(BuildJobState.Active));
        env.StopServer();
        await env.WaitForBuildToRestartAsync();
        engine = env.Engines.Get(EngineId1);
        Assert.That(engine.CurrentBuild, Is.Not.Null);
        Assert.That(engine.CurrentBuild.JobState, Is.EqualTo(BuildJobState.Pending));
        _ = env.PlatformService.Received().BuildRestartingAsync(BuildId1);
        env.SmtBatchTrainer.ClearSubstitute(ClearOptions.CallActions);
        env.StartServer();
        await env.WaitForBuildToFinishAsync();
        engine = env.Engines.Get(EngineId1);
        Assert.That(engine.CurrentBuild, Is.Null);
    }

    [TestCase(BuildJobRunnerType.Hangfire)]
    [TestCase(BuildJobRunnerType.ClearML)]
    public async Task DeleteAsync_WhileBuilding(BuildJobRunnerType trainJobRunnerType)
    {
        using var env = new TestEnvironment(trainJobRunnerType);
        env.UseInfiniteTrainJob();

        await env.Service.StartBuildAsync(EngineId1, BuildId1, "{}", Array.Empty<Corpus>());
        await env.WaitForTrainingToStartAsync();
        TranslationEngine engine = env.Engines.Get(EngineId1);
        Assert.That(engine.CurrentBuild, Is.Not.Null);
        Assert.That(engine.CurrentBuild.JobState, Is.EqualTo(BuildJobState.Active));
        await env.Service.DeleteAsync(EngineId1);
        await env.WaitForBuildToFinishAsync();
        await env.WaitForAllHangfireJobsToFinishAsync();
        _ = env.SmtBatchTrainer.DidNotReceive().SaveAsync();
        _ = env.TruecaserTrainer.DidNotReceive().SaveAsync();
        Assert.That(env.Engines.Contains(EngineId1), Is.False);
    }

    [TestCase(BuildJobRunnerType.Hangfire)]
    [TestCase(BuildJobRunnerType.ClearML)]
    public async Task TrainSegmentPairAsync(BuildJobRunnerType trainJobRunnerType)
    {
        using var env = new TestEnvironment(trainJobRunnerType);
        env.UseInfiniteTrainJob();

        await env.Service.StartBuildAsync(EngineId1, BuildId1, "{}", Array.Empty<Corpus>());
        await env.WaitForBuildToStartAsync();
        TranslationEngine engine = env.Engines.Get(EngineId1);
        Assert.That(engine.CurrentBuild, Is.Not.Null);
        Assert.That(engine.CurrentBuild.JobState, Is.EqualTo(BuildJobState.Active));
        await env.Service.TrainSegmentPairAsync(EngineId1, "esto es una prueba.", "this is a test.", true);
        env.StopTraining();
        await env.WaitForBuildToFinishAsync();
        engine = env.Engines.Get(EngineId1);
        Assert.That(engine.CurrentBuild, Is.Null);
        Assert.That(engine.BuildRevision, Is.EqualTo(2));
        _ = env.SmtModel.Received(2).TrainSegmentAsync("esto es una prueba.", "this is a test.", true);
    }

    [Test]
    public async Task CommitAsync_LoadedInactive()
    {
        using var env = new TestEnvironment();
        await env.Service.TrainSegmentPairAsync(EngineId1, "esto es una prueba.", "this is a test.", true);
        await Task.Delay(10);
        await env.CommitAsync(TimeSpan.Zero);
        _ = env.SmtModel.Received().SaveAsync();
        Assert.That(env.StateService.Get(EngineId1).IsLoaded, Is.False);
    }

    [Test]
    public async Task CommitAsync_LoadedActive()
    {
        using var env = new TestEnvironment();
        await env.Service.TrainSegmentPairAsync(EngineId1, "esto es una prueba.", "this is a test.", true);
        await env.CommitAsync(TimeSpan.FromHours(1));
        _ = env.SmtModel.Received().SaveAsync();
        Assert.That(env.StateService.Get(EngineId1).IsLoaded, Is.True);
    }

    [Test]
    public async Task TranslateAsync()
    {
        using var env = new TestEnvironment();
        TranslationResult result = (await env.Service.TranslateAsync(EngineId1, n: 1, "esto es una prueba."))[0];
        Assert.That(result.Translation, Is.EqualTo("this is a TEST."));
    }

    [Test]
    public async Task GetWordGraphAsync()
    {
        using var env = new TestEnvironment();
        WordGraph result = await env.Service.GetWordGraphAsync(EngineId1, "esto es una prueba.");
        Assert.That(
            result.Arcs.Select(a => string.Join(' ', a.TargetTokens)),
            Is.EqualTo(new[] { "this is", "a test", "." })
        );
    }

    private class TestEnvironment : ObjectModel.DisposableBase
    {
        private readonly Hangfire.InMemory.InMemoryStorage _memoryStorage;
        private readonly BackgroundJobClient _jobClient;
        private BackgroundJobServer _jobServer;
        private readonly ITruecaserFactory _truecaserFactory;
        private readonly IDistributedReaderWriterLockFactory _lockFactory;
        private readonly BuildJobRunnerType _trainJobRunnerType;
        private Task? _trainJobTask;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private bool _training = true;

        public TestEnvironment(BuildJobRunnerType trainJobRunnerType = BuildJobRunnerType.ClearML)
        {
            _trainJobRunnerType = trainJobRunnerType;
            Engines = new MemoryRepository<TranslationEngine>();
            Engines.Add(
                new TranslationEngine
                {
                    Id = EngineId1,
                    EngineId = EngineId1,
                    Type = TranslationEngineType.SmtTransfer,
                    SourceLanguage = "es",
                    TargetLanguage = "en",
                    BuildRevision = 1,
                    IsModelPersisted = false
                }
            );
            TrainSegmentPairs = new MemoryRepository<TrainSegmentPair>();
            _memoryStorage = new Hangfire.InMemory.InMemoryStorage();
            _jobClient = new BackgroundJobClient(_memoryStorage);
            PlatformService = Substitute.For<IPlatformService>();
            SmtModel = Substitute.For<IInteractiveTranslationModel>();
            SmtBatchTrainer = Substitute.For<ITrainer>();
            SmtBatchTrainer.Stats.Returns(
                new TrainStats { TrainCorpusSize = 0, Metrics = { { "bleu", 0.0 }, { "perplexity", 0.0 } } }
            );
            Truecaser = Substitute.For<ITruecaser>();
            TruecaserTrainer = Substitute.For<ITrainer>();

            SmtModelFactory = CreateSmtModelFactory();
            TransferEngineFactory = CreateTransferEngineFactory();
            _truecaserFactory = CreateTruecaserFactory();
            _lockFactory = new DistributedReaderWriterLockFactory(
                new OptionsWrapper<ServiceOptions>(new ServiceOptions { ServiceId = "host" }),
                new MemoryRepository<RWLock>(),
                new ObjectIdGenerator()
            );
            SharedFileService = new SharedFileService(Substitute.For<ILoggerFactory>());
            var clearMLOptions = Substitute.For<IOptionsMonitor<ClearMLOptions>>();
            clearMLOptions.CurrentValue.Returns(new ClearMLOptions());
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
                .Do(_ => _trainJobTask = Task.Run(RunTrainJob));
            ClearMLService
                .When(x => x.StopTaskAsync("job1", Arg.Any<CancellationToken>()))
                .Do(_ => _cancellationTokenSource.Cancel());
            ClearMLMonitorService = new ClearMLMonitorService(
                Substitute.For<IServiceProvider>(),
                ClearMLService,
                SharedFileService,
                clearMLOptions,
                buildJobOptions,
                Substitute.For<ILogger<ClearMLMonitorService>>()
            );
            BuildJobService = new BuildJobService(
                [
                    new HangfireBuildJobRunner(_jobClient, [new SmtTransferHangfireBuildJobFactory()]),
                    new ClearMLBuildJobRunner(
                        ClearMLService,
                        [new SmtTransferClearMLBuildJobFactory(SharedFileService, Engines)],
                        buildJobOptions
                    )
                ],
                Engines
            );
            _jobServer = CreateJobServer();
            StateService = CreateStateService();
            Service = CreateService();
        }

        public SmtTransferEngineService Service { get; private set; }
        public SmtTransferEngineStateService StateService { get; private set; }
        public MemoryRepository<TranslationEngine> Engines { get; }
        public MemoryRepository<TrainSegmentPair> TrainSegmentPairs { get; }
        public ISmtModelFactory SmtModelFactory { get; }
        public ITransferEngineFactory TransferEngineFactory { get; }
        public ITrainer SmtBatchTrainer { get; }
        public IInteractiveTranslationModel SmtModel { get; }
        public ITruecaser Truecaser { get; }
        public ITrainer TruecaserTrainer { get; }
        public IPlatformService PlatformService { get; }

        public IClearMLService ClearMLService { get; }
        public IClearMLQueueService ClearMLMonitorService { get; }

        public ISharedFileService SharedFileService { get; }

        public IBuildJobService BuildJobService { get; }

        public async Task CommitAsync(TimeSpan inactiveTimeout)
        {
            await StateService.CommitAsync(_lockFactory, Engines, inactiveTimeout);
        }

        public void StopServer()
        {
            _jobServer.Dispose();
            StateService.Dispose();
        }

        public void StartServer()
        {
            _jobServer = CreateJobServer();
            StateService = CreateStateService();
            Service = CreateService();
        }

        public void UseInfiniteTrainJob()
        {
            SmtBatchTrainer.TrainAsync(
                Arg.Any<IProgress<ProgressStatus>>(),
                Arg.Do<CancellationToken>(cancellationToken =>
                {
                    while (_training)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Thread.Sleep(100);
                    }
                })
            );
        }

        public void StopTraining()
        {
            _training = false;
        }

        private BackgroundJobServer CreateJobServer()
        {
            var jobServerOptions = new BackgroundJobServerOptions
            {
                Activator = new EnvActivator(this),
                Queues = new[] { "smt_transfer" },
                CancellationCheckInterval = TimeSpan.FromMilliseconds(50),
            };
            return new BackgroundJobServer(jobServerOptions, _memoryStorage);
        }

        private SmtTransferEngineStateService CreateStateService()
        {
            var options = Substitute.For<IOptionsMonitor<SmtTransferEngineOptions>>();
            options.CurrentValue.Returns(new SmtTransferEngineOptions());
            return new SmtTransferEngineStateService(
                SmtModelFactory,
                TransferEngineFactory,
                _truecaserFactory,
                options
            );
        }

        private SmtTransferEngineService CreateService()
        {
            return new SmtTransferEngineService(
                _lockFactory,
                PlatformService,
                new MemoryDataAccessContext(),
                Engines,
                TrainSegmentPairs,
                StateService,
                BuildJobService,
                ClearMLMonitorService
            );
        }

        private ISmtModelFactory CreateSmtModelFactory()
        {
            ISmtModelFactory factory = Substitute.For<ISmtModelFactory>();

            var translationResult = new TranslationResult(
                "this is a TEST.",
                "esto es una prueba .".Split(),
                "this is a TEST .".Split(),
                [1.0, 1.0, 1.0, 1.0, 1.0],
                [
                    TranslationSources.Smt,
                    TranslationSources.Smt,
                    TranslationSources.Smt,
                    TranslationSources.Smt,
                    TranslationSources.Smt
                ],
                new WordAlignmentMatrix(5, 5)
                {
                    [0, 0] = true,
                    [1, 1] = true,
                    [2, 2] = true,
                    [3, 3] = true,
                    [4, 4] = true
                },
                [new Phrase(Range<int>.Create(0, 5), 5)]
            );
            SmtModel
                .TranslateAsync(1, Arg.Any<string>())
                .Returns(Task.FromResult<IReadOnlyList<TranslationResult>>([translationResult]));
            SmtModel
                .GetWordGraphAsync(Arg.Any<string>())
                .Returns(
                    Task.FromResult(
                        new WordGraph(
                            "esto es una prueba .".Split(),
                            new[]
                            {
                                new WordGraphArc(
                                    0,
                                    1,
                                    1.0,
                                    "this is".Split(),
                                    new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                                    Range<int>.Create(0, 2),
                                    GetSources(2, false),
                                    [1.0, 1.0]
                                ),
                                new WordGraphArc(
                                    1,
                                    2,
                                    1.0,
                                    "a test".Split(),
                                    new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                                    Range<int>.Create(2, 4),
                                    GetSources(2, false),
                                    [1.0, 1.0]
                                ),
                                new WordGraphArc(
                                    2,
                                    3,
                                    1.0,
                                    ".".Split(),
                                    new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                                    Range<int>.Create(4, 5),
                                    GetSources(1, false),
                                    [1.0]
                                )
                            },
                            [3]
                        )
                    )
                );

            factory
                .CreateAsync(
                    Arg.Any<string>(),
                    Arg.Any<IRangeTokenizer<string, int, string>>(),
                    Arg.Any<IDetokenizer<string, string>>(),
                    Arg.Any<ITruecaser>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns(Task.FromResult(SmtModel));
            factory
                .CreateTrainerAsync(
                    Arg.Any<string>(),
                    Arg.Any<IRangeTokenizer<string, int, string>>(),
                    Arg.Any<IParallelTextCorpus>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns(Task.FromResult(SmtBatchTrainer));
            return factory;
        }

        private static ITransferEngineFactory CreateTransferEngineFactory()
        {
            ITransferEngineFactory factory = Substitute.For<ITransferEngineFactory>();
            ITranslationEngine engine = Substitute.For<ITranslationEngine>();
            engine
                .TranslateAsync(Arg.Any<string>())
                .Returns(
                    Task.FromResult(
                        new TranslationResult(
                            "this is a TEST.",
                            "esto es una prueba .".Split(),
                            "this is a TEST .".Split(),
                            [1.0, 1.0, 1.0, 1.0, 1.0],
                            [
                                TranslationSources.Transfer,
                                TranslationSources.Transfer,
                                TranslationSources.Transfer,
                                TranslationSources.Transfer,
                                TranslationSources.Transfer
                            ],
                            new WordAlignmentMatrix(5, 5)
                            {
                                [0, 0] = true,
                                [1, 1] = true,
                                [2, 2] = true,
                                [3, 3] = true,
                                [4, 4] = true
                            },
                            [new Phrase(Range<int>.Create(0, 5), 5)]
                        )
                    )
                );
            factory
                .CreateAsync(
                    Arg.Any<string>(),
                    Arg.Any<IRangeTokenizer<string, int, string>>(),
                    Arg.Any<IDetokenizer<string, string>>(),
                    Arg.Any<ITruecaser>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns(Task.FromResult<ITranslationEngine?>(engine));
            return factory;
        }

        private ITruecaserFactory CreateTruecaserFactory()
        {
            ITruecaserFactory factory = Substitute.For<ITruecaserFactory>();
            factory.CreateAsync(Arg.Any<string>()).Returns(Task.FromResult(Truecaser));
            factory
                .CreateTrainerAsync(
                    Arg.Any<string>(),
                    Arg.Any<ITokenizer<string, int, string>>(),
                    Arg.Any<ITextCorpus>(),
                    Arg.Any<CancellationToken>()
                )
                .Returns(Task.FromResult(TruecaserTrainer));
            return factory;
        }

        private static TranslationSources[] GetSources(int count, bool isUnknown)
        {
            var sources = new TranslationSources[count];
            for (int i = 0; i < count; i++)
                sources[i] = isUnknown ? TranslationSources.None : TranslationSources.Smt;
            return sources;
        }

        public async Task WaitForAllHangfireJobsToFinishAsync()
        {
            IMonitoringApi monitoringApi = _memoryStorage.GetMonitoringApi();
            while (monitoringApi.EnqueuedCount("smt_transfer") > 0 || monitoringApi.ProcessingCount() > 0)
                await Task.Delay(50);
        }

        public async Task WaitForBuildToFinishAsync()
        {
            await WaitForBuildState(e => e.CurrentBuild is null);
            if (_trainJobTask is not null)
                await _trainJobTask;
        }

        public Task WaitForBuildToStartAsync()
        {
            return WaitForBuildState(e => e.CurrentBuild!.JobState is BuildJobState.Active);
        }

        public Task WaitForTrainingToStartAsync()
        {
            return WaitForBuildState(e =>
                e.CurrentBuild!.JobState is BuildJobState.Active && e.CurrentBuild!.Stage is BuildStage.Train
            );
        }

        public Task WaitForBuildToRestartAsync()
        {
            return WaitForBuildState(e => e.CurrentBuild!.JobState is BuildJobState.Pending);
        }

        private async Task WaitForBuildState(Func<TranslationEngine, bool> predicate)
        {
            using ISubscription<TranslationEngine> subscription = await Engines.SubscribeAsync(e =>
                e.EngineId == EngineId1
            );
            while (true)
            {
                TranslationEngine? engine = subscription.Change.Entity;
                if (engine is null || predicate(engine))
                    break;
                await subscription.WaitForChangeAsync();
            }
        }

        protected override void DisposeManagedResources()
        {
            StateService.Dispose();
            _jobServer.Dispose();
        }

        private async Task RunTrainJob()
        {
            try
            {
                await BuildJobService.BuildJobStartedAsync("engine1", "build1", _cancellationTokenSource.Token);

                string engineDir = Path.Combine("translation_engines", EngineId1);
                await SmtModelFactory.InitNewAsync(engineDir, _cancellationTokenSource.Token);
                ITextCorpus sourceCorpus = new DictionaryTextCorpus();
                ITextCorpus targetCorpus = new DictionaryTextCorpus();
                IParallelTextCorpus parallelCorpus = sourceCorpus.AlignRows(targetCorpus);
                LatinWordTokenizer tokenizer = new();
                using ITrainer smtModelTrainer = await SmtModelFactory.CreateTrainerAsync(
                    engineDir,
                    tokenizer,
                    parallelCorpus,
                    _cancellationTokenSource.Token
                );
                using ITrainer truecaseTrainer = await _truecaserFactory.CreateTrainerAsync(
                    engineDir,
                    tokenizer,
                    targetCorpus,
                    _cancellationTokenSource.Token
                );
                await smtModelTrainer.TrainAsync(null, _cancellationTokenSource.Token);
                await truecaseTrainer.TrainAsync(cancellationToken: _cancellationTokenSource.Token);

                await smtModelTrainer.SaveAsync(_cancellationTokenSource.Token);
                await truecaseTrainer.SaveAsync(_cancellationTokenSource.Token);

                await using Stream engineStream = await SharedFileService.OpenWriteAsync(
                    $"builds/{BuildId1}/model.tar.gz",
                    _cancellationTokenSource.Token
                );

                await using Stream targetStream = await SharedFileService.OpenWriteAsync(
                    $"builds/{BuildId1}/pretranslate.trg.json",
                    _cancellationTokenSource.Token
                );

                await BuildJobService.StartBuildJobAsync(
                    BuildJobRunnerType.Hangfire,
                    EngineId1,
                    BuildId1,
                    BuildStage.Postprocess,
                    data: (0, 0.0)
                );
            }
            catch (OperationCanceledException)
            {
                await BuildJobService.BuildJobFinishedAsync("engine1", "build1", buildComplete: false);
            }
        }

        private class EnvActivator(TestEnvironment env) : JobActivator
        {
            private readonly TestEnvironment _env = env;

            public override object ActivateJob(Type jobType)
            {
                if (jobType == typeof(PreprocessBuildJob))
                {
                    return new PreprocessBuildJob(
                        _env.PlatformService,
                        _env.Engines,
                        _env._lockFactory,
                        Substitute.For<ILogger<PreprocessBuildJob>>(),
                        _env.BuildJobService,
                        _env.SharedFileService,
                        Substitute.For<ICorpusService>()
                    )
                    {
                        TrainJobRunnerType = _env._trainJobRunnerType
                    };
                }
                if (jobType == typeof(SmtTransferPostprocessBuildJob))
                {
                    var options = Substitute.For<IOptionsMonitor<SmtTransferEngineOptions>>();
                    options.CurrentValue.Returns(new SmtTransferEngineOptions());
                    return new SmtTransferPostprocessBuildJob(
                        _env.PlatformService,
                        _env.Engines,
                        _env._lockFactory,
                        _env.BuildJobService,
                        Substitute.For<ILogger<SmtTransferPostprocessBuildJob>>(),
                        _env.SharedFileService,
                        _env.TrainSegmentPairs,
                        _env.SmtModelFactory,
                        _env._truecaserFactory,
                        options
                    );
                }
                if (jobType == typeof(SmtTransferTrainBuildJob))
                {
                    return new SmtTransferTrainBuildJob(
                        _env.PlatformService,
                        _env.Engines,
                        _env._lockFactory,
                        _env.BuildJobService,
                        Substitute.For<ILogger<SmtTransferTrainBuildJob>>(),
                        _env.SharedFileService,
                        _env._truecaserFactory,
                        _env.SmtModelFactory,
                        _env.TransferEngineFactory
                    );
                }
                return base.ActivateJob(jobType);
            }
        }
    }
}
