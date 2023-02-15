namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class SmtTransferEngineRuntimeTests
{
    [Test]
    public async Task StartBuildAsync()
    {
        using var env = new TestEnvironment();
        TranslationEngine engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildRevision, Is.EqualTo(0));
        await env.Runtime.InitNewAsync();
        // ensure that the SMT model was loaded before training
        await env.Runtime.TranslateAsync(n: 1, "esto es una prueba.");
        await env.Runtime.StartBuildAsync("build1");
        await env.WaitForBuildToStartAsync();
        engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildState, Is.EqualTo(BuildState.Active));
        await env.WaitForBuildToFinishAsync();
        await env.SmtBatchTrainer
            .Received()
            .TrainAsync(Arg.Any<IProgress<ProgressStatus>>(), Arg.Any<CancellationToken>());
        await env.TruecaserTrainer
            .Received()
            .TrainAsync(Arg.Any<IProgress<ProgressStatus>>(), Arg.Any<CancellationToken>());
        await env.SmtBatchTrainer.Received().SaveAsync();
        await env.TruecaserTrainer.Received().SaveAsync();
        engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildState, Is.EqualTo(BuildState.None));
        Assert.That(engine.BuildRevision, Is.EqualTo(1));
        // check if SMT model was reloaded upon first use after training
        env.SmtModel.ClearReceivedCalls();
        await env.Runtime.TranslateAsync(n: 1, "esto es una prueba.");
        env.SmtModel.Received().Dispose();
        await env.SmtModel.DidNotReceive().SaveAsync();
        await env.Truecaser.DidNotReceive().SaveAsync();
    }

    [Test]
    public async Task CancelBuildAsync()
    {
        using var env = new TestEnvironment();
        await env.Runtime.InitNewAsync();
        await env.SmtBatchTrainer.TrainAsync(
            Arg.Any<IProgress<ProgressStatus>>(),
            Arg.Do<CancellationToken>(
                ct =>
                {
                    while (true)
                        ct.ThrowIfCancellationRequested();
                }
            )
        );
        await env.Runtime.StartBuildAsync("build1");
        await env.WaitForBuildToStartAsync();
        TranslationEngine engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildState, Is.EqualTo(BuildState.Active));
        await env.Runtime.CancelBuildAsync();
        await env.WaitForBuildToFinishAsync();
        await env.SmtBatchTrainer.DidNotReceive().SaveAsync();
        await env.TruecaserTrainer.DidNotReceive().SaveAsync();
        engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildState, Is.EqualTo(BuildState.None));
    }

    [Test]
    public async Task StartBuildAsync_RestartUnfinishedBuild()
    {
        using var env = new TestEnvironment();
        await env.Runtime.InitNewAsync();
        await env.SmtBatchTrainer.TrainAsync(
            Arg.Any<IProgress<ProgressStatus>>(),
            Arg.Do<CancellationToken>(
                ct =>
                {
                    while (true)
                        ct.ThrowIfCancellationRequested();
                }
            )
        );
        await env.Runtime.StartBuildAsync("build1");
        await env.WaitForBuildToStartAsync();
        TranslationEngine engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildState, Is.EqualTo(BuildState.Active));
        env.StopServer();
        engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildState, Is.EqualTo(BuildState.Pending));
        await env.PlatformService.Received().BuildRestartingAsync("build1");
        env.SmtBatchTrainer.ClearSubstitute(ClearOptions.CallActions);
        env.StartServer();
        await env.WaitForBuildToFinishAsync();
        engine = env.Engines.Get("engine1");
        Assert.That(engine.BuildState, Is.EqualTo(BuildState.None));
    }

    [Test]
    public async Task CommitAsync_LoadedInactive()
    {
        using var env = new TestEnvironment();
        env.EngineOptions.InactiveEngineTimeout = TimeSpan.Zero;
        await env.Runtime.InitNewAsync();
        await env.Runtime.TrainSegmentPairAsync("esto es una prueba.", "this is a test.", true);
        await Task.Delay(10);
        await env.Runtime.CommitAsync();
        await env.SmtModel.Received().SaveAsync();
        env.Truecaser
            .Received()
            .TrainSegment(Arg.Is<IReadOnlyList<string>>(x => x.SequenceEqual("this is a test .".Split())), true);
        Assert.That(env.Runtime.IsLoaded, Is.False);
    }

    [Test]
    public async Task CommitAsync_LoadedActive()
    {
        using var env = new TestEnvironment();
        env.EngineOptions.InactiveEngineTimeout = TimeSpan.FromHours(1);
        await env.Runtime.InitNewAsync();
        await env.Runtime.TrainSegmentPairAsync("esto es una prueba.", "this is a test.", true);
        await env.Runtime.CommitAsync();
        await env.SmtModel.Received().SaveAsync();
        env.Truecaser
            .Received()
            .TrainSegment(Arg.Is<IReadOnlyList<string>>(x => x.SequenceEqual("this is a test .".Split())), true);
        Assert.That(env.Runtime.IsLoaded, Is.True);
    }

    [Test]
    public async Task TranslateAsync()
    {
        using var env = new TestEnvironment();
        env.EngineOptions.InactiveEngineTimeout = TimeSpan.FromHours(1);
        await env.Runtime.InitNewAsync();
        (string translation, _) = (await env.Runtime.TranslateAsync(n: 1, "esto es una prueba."))[0];
        Assert.That(translation, Is.EqualTo("this is a TEST."));
    }

    private class TestEnvironment : DisposableBase
    {
        private readonly MemoryStorage _memoryStorage;
        private readonly BackgroundJobClient _jobClient;
        private BackgroundJobServer _jobServer;
        private readonly ISmtModelFactory _smtModelFactory;
        private readonly ITransferEngineFactory _transferEngineFactory;
        private readonly ITruecaserFactory _truecaserFactory;
        private readonly IDistributedReaderWriterLockFactory _lockFactory;

        public TestEnvironment()
        {
            Engines = new MemoryRepository<TranslationEngine>();
            Engines.Add(new TranslationEngine { Id = "engine1", EngineId = "engine1" });
            TrainSegmentPairs = new MemoryRepository<TrainSegmentPair>();
            EngineOptions = new TranslationEngineOptions();
            _memoryStorage = new MemoryStorage();
            _jobClient = new BackgroundJobClient(_memoryStorage);
            PlatformService = Substitute.For<IPlatformService>();
            SmtModel = Substitute.For<IInteractiveTranslationModel>();
            SmtBatchTrainer = Substitute.For<ITrainer>();
            SmtBatchTrainer.Stats.Returns(new TrainStats { Metrics = { { "bleu", 0.0 }, { "perplexity", 0.0 } } });
            Truecaser = Substitute.For<ITruecaser>();
            TruecaserTrainer = Substitute.For<ITrainer>();
            TruecaserTrainer.SaveAsync().Returns(Task.CompletedTask);
            _smtModelFactory = CreateSmtModelFactory();
            _transferEngineFactory = CreateTransferEngineFactory();
            _truecaserFactory = CreateTruecaserFactory();
            _lockFactory = new DistributedReaderWriterLockFactory(
                new OptionsWrapper<ServiceOptions>(new ServiceOptions { ServiceId = "host" }),
                new MemoryRepository<RWLock>()
            );
            _jobServer = CreateJobServer();
            Runtime = CreateRuntime();
        }

        public SmtTransferEngineRuntime Runtime { get; private set; }
        public MemoryRepository<TranslationEngine> Engines { get; }
        public MemoryRepository<TrainSegmentPair> TrainSegmentPairs { get; }
        public ITrainer SmtBatchTrainer { get; }
        public IInteractiveTranslationModel SmtModel { get; }
        public TranslationEngineOptions EngineOptions { get; }
        public ITruecaser Truecaser { get; }
        public ITrainer TruecaserTrainer { get; }
        public IPlatformService PlatformService { get; }

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
                Queues = new[] { "smt_transfer" },
                CancellationCheckInterval = TimeSpan.FromMilliseconds(50),
            };
            return new BackgroundJobServer(jobServerOptions, _memoryStorage);
        }

        private SmtTransferEngineRuntime CreateRuntime()
        {
            var engineOptions = Substitute.For<IOptionsMonitor<TranslationEngineOptions>>();
            engineOptions.CurrentValue.Returns(EngineOptions);
            return new SmtTransferEngineRuntime(
                engineOptions,
                PlatformService,
                Engines,
                TrainSegmentPairs,
                _smtModelFactory,
                _transferEngineFactory,
                _truecaserFactory,
                _jobClient,
                _lockFactory,
                "engine1"
            );
        }

        private ISmtModelFactory CreateSmtModelFactory()
        {
            var factory = Substitute.For<ISmtModelFactory>();

            var translationResult = new TranslationResult(
                5,
                "this is a test .".Split(),
                new[] { 1.0, 1.0, 1.0, 1.0, 1.0 },
                new[]
                {
                    TranslationSources.Smt,
                    TranslationSources.Smt,
                    TranslationSources.Smt,
                    TranslationSources.Smt,
                    TranslationSources.Smt
                },
                new WordAlignmentMatrix(5, 5)
                {
                    [0, 0] = true,
                    [1, 1] = true,
                    [2, 2] = true,
                    [3, 3] = true,
                    [4, 4] = true
                },
                new[] { new Phrase(Range<int>.Create(0, 5), 5, 1.0) }
            );
            SmtModel
                .TranslateAsync(1, Arg.Any<IReadOnlyList<string>>())
                .Returns(Task.FromResult<IReadOnlyList<TranslationResult>>(new[] { translationResult }));
            SmtModel
                .GetWordGraphAsync(Arg.Any<IReadOnlyList<string>>())
                .Returns(
                    Task.FromResult(
                        new WordGraph(
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
                                    new[] { 1.0, 1.0 }
                                ),
                                new WordGraphArc(
                                    1,
                                    2,
                                    1.0,
                                    "a test".Split(),
                                    new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true },
                                    Range<int>.Create(2, 4),
                                    GetSources(2, false),
                                    new[] { 1.0, 1.0 }
                                ),
                                new WordGraphArc(
                                    2,
                                    3,
                                    1.0,
                                    new[] { "." },
                                    new WordAlignmentMatrix(1, 1) { [0, 0] = true },
                                    Range<int>.Create(4, 5),
                                    GetSources(1, false),
                                    new[] { 1.0 }
                                )
                            },
                            new[] { 3 }
                        )
                    )
                );

            factory.Create(Arg.Any<string>()).Returns(SmtModel);
            factory.CreateTrainer(Arg.Any<string>(), Arg.Any<IParallelTextCorpus>()).Returns(SmtBatchTrainer);
            return factory;
        }

        private static ITransferEngineFactory CreateTransferEngineFactory()
        {
            var factory = Substitute.For<ITransferEngineFactory>();
            var engine = Substitute.For<ITranslationEngine>();
            engine
                .TranslateAsync(Arg.Any<IReadOnlyList<string>>())
                .Returns(
                    Task.FromResult(
                        new TranslationResult(
                            5,
                            "this is a test .".Split(),
                            new[] { 1.0, 1.0, 1.0, 1.0, 1.0 },
                            new[]
                            {
                                TranslationSources.Transfer,
                                TranslationSources.Transfer,
                                TranslationSources.Transfer,
                                TranslationSources.Transfer,
                                TranslationSources.Transfer
                            },
                            new WordAlignmentMatrix(5, 5)
                            {
                                [0, 0] = true,
                                [1, 1] = true,
                                [2, 2] = true,
                                [3, 3] = true,
                                [4, 4] = true
                            },
                            new[] { new Phrase(Range<int>.Create(0, 5), 5, 1.0) }
                        )
                    )
                );
            factory.Create(Arg.Any<string>()).Returns(engine);
            return factory;
        }

        private ITruecaserFactory CreateTruecaserFactory()
        {
            var factory = Substitute.For<ITruecaserFactory>();
            Truecaser
                .Truecase(Arg.Any<IReadOnlyList<string>>())
                .Returns(
                    x =>
                    {
                        var segment = x.Arg<IReadOnlyList<string>>();
                        return segment.Select(t => t == "test" ? "TEST" : t).ToArray();
                    }
                );
            factory.CreateAsync(Arg.Any<string>()).Returns(Task.FromResult(Truecaser));
            factory.CreateTrainer(Arg.Any<string>(), Arg.Any<ITextCorpus>()).Returns(TruecaserTrainer);
            return factory;
        }

        private static IEnumerable<TranslationSources> GetSources(int count, bool isUnknown)
        {
            var sources = new TranslationSources[count];
            for (int i = 0; i < count; i++)
                sources[i] = isUnknown ? TranslationSources.None : TranslationSources.Smt;
            return sources;
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
                if (jobType == typeof(SmtTransferEngineBuildJob))
                {
                    return new SmtTransferEngineBuildJob(
                        _env.PlatformService,
                        _env.Engines,
                        _env.TrainSegmentPairs,
                        _env._lockFactory,
                        _env._truecaserFactory,
                        _env._smtModelFactory,
                        Substitute.For<ILogger<SmtTransferEngineBuildJob>>()
                    );
                }
                return base.ActivateJob(jobType);
            }
        }
    }
}
