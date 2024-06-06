namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class NmtPreprocessBuildJobTests
{
    [Test]
    public async Task RunAsync_FilterOutEverything()
    {
        using TestEnvironment env = new();
        Corpus corpus1 = env.DefaultTextFileCorpus with { };

        await env.RunBuildJobAsync(corpus1);

        (int src1Count, int src2Count, int trgCount, int termCount) = await env.GetTrainCountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(src1Count, Is.EqualTo(0));
            Assert.That(src2Count, Is.EqualTo(0));
            Assert.That(trgCount, Is.EqualTo(0));
            Assert.That(termCount, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task RunAsync_TrainOnAll()
    {
        using TestEnvironment env = new();
        Corpus corpus1 = env.DefaultTextFileCorpus with { TrainOnAll = true };

        await env.RunBuildJobAsync(corpus1);

        (int src1Count, int src2Count, int trgCount, int termCount) = await env.GetTrainCountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(src1Count, Is.EqualTo(4));
            Assert.That(src2Count, Is.EqualTo(0));
            Assert.That(trgCount, Is.EqualTo(1));
            Assert.That(termCount, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task RunAsync_TrainOnTextIds()
    {
        using TestEnvironment env = new();
        Corpus corpus1 = env.DefaultTextFileCorpus with { TrainOnTextIds = ["textId1"] };

        await env.RunBuildJobAsync(corpus1);

        (int src1Count, int src2Count, int trgCount, int termCount) = await env.GetTrainCountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(src1Count, Is.EqualTo(4));
            Assert.That(src2Count, Is.EqualTo(0));
            Assert.That(trgCount, Is.EqualTo(1));
            Assert.That(termCount, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task RunAsync_TrainAndPretranslateAll()
    {
        using TestEnvironment env = new();
        Corpus corpus1 = env.DefaultTextFileCorpus with { PretranslateAll = true, TrainOnAll = true };

        await env.RunBuildJobAsync(corpus1);

        Assert.That(await env.GetPretranslateCountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task RunAsync_PretranslateAll()
    {
        using TestEnvironment env = new();
        Corpus corpus1 = env.DefaultTextFileCorpus with { PretranslateAll = true };

        await env.RunBuildJobAsync(corpus1);

        Assert.That(await env.GetPretranslateCountAsync(), Is.EqualTo(4));
    }

    [Test]
    public async Task RunAsync_PretranslateTextIds()
    {
        using TestEnvironment env = new();
        Corpus corpus1 = env.DefaultTextFileCorpus with { PretranslateTextIds = ["textId1"], TrainOnAll = true };

        await env.RunBuildJobAsync(corpus1);

        Assert.That(await env.GetPretranslateCountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task RunAsync_EnableKeyTerms()
    {
        using TestEnvironment env = new();
        Corpus corpus1 = env.DefaultParatextCorpus with { };

        await env.RunBuildJobAsync(corpus1, useKeyTerms: true);

        (int src1Count, int src2Count, int trgCount, int termCount) = await env.GetTrainCountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(src1Count, Is.EqualTo(0));
            Assert.That(src2Count, Is.EqualTo(0));
            Assert.That(trgCount, Is.EqualTo(0));
            Assert.That(termCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task RunAsync_DisableKeyTerms()
    {
        using TestEnvironment env = new();
        Corpus corpus1 = env.DefaultParatextCorpus with { };

        await env.RunBuildJobAsync(corpus1, useKeyTerms: false);

        (int src1Count, int src2Count, int trgCount, int termCount) = await env.GetTrainCountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(src1Count, Is.EqualTo(0));
            Assert.That(src2Count, Is.EqualTo(0));
            Assert.That(trgCount, Is.EqualTo(0));
            Assert.That(termCount, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task RunAsync_PretranslateChapters()
    {
        using TestEnvironment env = new();
        Corpus corpus1 = env.DefaultParatextCorpus with
        {
            PretranslateChapters = new Dictionary<string, HashSet<int>>
            {
                {
                    "1CH",
                    new HashSet<int> { 12 }
                }
            }
        };

        await env.RunBuildJobAsync(corpus1);

        Assert.That(await env.GetPretranslateCountAsync(), Is.EqualTo(4));
    }

    [Test]
    public async Task RunAsync_TrainOnChapters()
    {
        using TestEnvironment env = new();
        Corpus corpus1 = env.DefaultParatextCorpus with
        {
            TrainOnChapters = new Dictionary<string, HashSet<int>>
            {
                {
                    "MAT",
                    new HashSet<int> { 1 }
                }
            }
        };

        await env.RunBuildJobAsync(corpus1, useKeyTerms: false);

        (int src1Count, int src2Count, int trgCount, int termCount) = await env.GetTrainCountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(src1Count, Is.EqualTo(5));
            Assert.That(src2Count, Is.EqualTo(0));
            Assert.That(trgCount, Is.EqualTo(0));
            Assert.That(termCount, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task RunAsync_MixedSource_Paratext()
    {
        using TestEnvironment env = new();
        Corpus corpus1 = env.DefaultMixedSourceParatextCorpus with { TrainOnAll = true, PretranslateAll = true };

        await env.RunBuildJobAsync(corpus1, useKeyTerms: false);

        (int src1Count, int src2Count, int trgCount, int termCount) = await env.GetTrainCountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(src1Count, Is.EqualTo(4));
            Assert.That(src2Count, Is.EqualTo(12));
            Assert.That(trgCount, Is.EqualTo(1));
            Assert.That(termCount, Is.EqualTo(0));
        });
        Assert.That(await env.GetPretranslateCountAsync(), Is.EqualTo(12));
    }

    [Test]
    public async Task RunAsync_MixedSource_Text()
    {
        using TestEnvironment env = new();
        Corpus corpus1 = env.DefaultMixedSourceTextFileCorpus with { TrainOnAll = true, PretranslateAll = true };

        await env.RunBuildJobAsync(corpus1);

        (int src1Count, int src2Count, int trgCount, int termCount) = await env.GetTrainCountAsync();
        Assert.Multiple(() =>
        {
            Assert.That(src1Count, Is.EqualTo(3));
            Assert.That(src2Count, Is.EqualTo(2));
            Assert.That(trgCount, Is.EqualTo(1));
            Assert.That(termCount, Is.EqualTo(0));
        });
        Assert.That(await env.GetPretranslateCountAsync(), Is.EqualTo(2));
    }

    [Test]
    public void RunAsync_UnknownLanguageTagsNoData()
    {
        using TestEnvironment env = new();
        Corpus corpus1 = env.DefaultTextFileCorpus with { SourceLanguage = "xxx", TargetLanguage = "zzz" };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await env.RunBuildJobAsync(corpus1, engineId: "engine2");
        });
    }

    private class TestEnvironment : ObjectModel.DisposableBase
    {
        private static readonly string TestDataPath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "Services",
            "data"
        );

        private readonly TempDirectory _tempDir;

        public ISharedFileService SharedFileService { get; }
        public ICorpusService CorpusService { get; }
        public IPlatformService PlatformService { get; }
        public MemoryRepository<TranslationEngine> Engines { get; }
        public IDistributedReaderWriterLockFactory LockFactory { get; }
        public IBuildJobService BuildJobService { get; }
        public ILogger<NmtPreprocessBuildJob> Logger { get; }
        public IClearMLService ClearMLService { get; }
        public NmtPreprocessBuildJob BuildJob { get; }
        public IOptionsMonitor<ClearMLOptions> Options { get; }

        public Corpus DefaultTextFileCorpus { get; }
        public Corpus DefaultMixedSourceTextFileCorpus { get; }
        public Corpus DefaultParatextCorpus { get; }
        public Corpus DefaultMixedSourceParatextCorpus { get; }

        public TestEnvironment()
        {
            if (!Sldr.IsInitialized)
                Sldr.Initialize(offlineMode: true);

            _tempDir = new TempDirectory("NmtPreprocessBuildJobTests");

            ZipParatextProject("pt-source1");
            ZipParatextProject("pt-source2");
            ZipParatextProject("pt-target1");

            DefaultTextFileCorpus = new()
            {
                Id = "corpusId1",
                SourceLanguage = "es",
                TargetLanguage = "en",
                PretranslateAll = false,
                TrainOnAll = false,
                PretranslateTextIds = [],
                TrainOnTextIds = [],
                SourceFiles = [TextFile("source1")],
                TargetFiles = [TextFile("target1")]
            };

            DefaultMixedSourceTextFileCorpus = new()
            {
                Id = "corpusId1",
                SourceLanguage = "es",
                TargetLanguage = "en",
                PretranslateAll = false,
                TrainOnAll = false,
                PretranslateTextIds = [],
                TrainOnTextIds = [],
                SourceFiles = [TextFile("source1"), TextFile("source2")],
                TargetFiles = [TextFile("target1")]
            };

            DefaultParatextCorpus = new()
            {
                Id = "corpusId1",
                SourceLanguage = "es",
                TargetLanguage = "en",
                PretranslateAll = false,
                TrainOnAll = false,
                PretranslateTextIds = [],
                TrainOnTextIds = [],
                SourceFiles = [ParatextFile("pt-source1")],
                TargetFiles = [ParatextFile("pt-target1")]
            };

            DefaultMixedSourceParatextCorpus = new()
            {
                Id = "corpusId1",
                SourceLanguage = "es",
                TargetLanguage = "en",
                PretranslateAll = false,
                TrainOnAll = false,
                PretranslateTextIds = [],
                TrainOnTextIds = [],
                SourceFiles = [ParatextFile("pt-source1"), ParatextFile("pt-source2")],
                TargetFiles = [ParatextFile("pt-target1")]
            };

            Engines = new MemoryRepository<TranslationEngine>();
            Engines.Add(
                new TranslationEngine
                {
                    Id = "engine1",
                    EngineId = "engine1",
                    SourceLanguage = "es",
                    TargetLanguage = "en",
                    BuildRevision = 1,
                    IsModelPersisted = false,
                    CurrentBuild = new()
                    {
                        BuildId = "build1",
                        JobId = "job1",
                        JobState = BuildJobState.Pending,
                        JobRunner = BuildJobRunner.Hangfire,
                        Stage = NmtBuildStages.Preprocess
                    }
                }
            );
            Engines.Add(
                new TranslationEngine
                {
                    Id = "engine2",
                    EngineId = "engine2",
                    SourceLanguage = "xxx",
                    TargetLanguage = "zzz",
                    BuildRevision = 1,
                    IsModelPersisted = false,
                    CurrentBuild = new()
                    {
                        BuildId = "build1",
                        JobId = "job1",
                        JobState = BuildJobState.Pending,
                        JobRunner = BuildJobRunner.Hangfire,
                        Stage = NmtBuildStages.Preprocess
                    }
                }
            );
            CorpusService = new CorpusService();
            PlatformService = Substitute.For<IPlatformService>();
            LockFactory = new DistributedReaderWriterLockFactory(
                new OptionsWrapper<ServiceOptions>(new ServiceOptions { ServiceId = "host" }),
                new MemoryRepository<RWLock>(),
                new ObjectIdGenerator()
            );
            Options = Substitute.For<IOptionsMonitor<ClearMLOptions>>();
            Options.CurrentValue.Returns(new ClearMLOptions { ModelType = "test_model" });
            ClearMLService = Substitute.For<IClearMLService>();
            ClearMLService
                .GetProjectIdAsync("engine1", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<string?>("project1"));
            ClearMLService
                .GetProjectIdAsync("engine2", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<string?>("project1"));
            ClearMLService
                .CreateTaskAsync("build1", "project1", Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("job1"));
            SharedFileService = new SharedFileService(Substitute.For<ILoggerFactory>());
            Logger = Substitute.For<ILogger<NmtPreprocessBuildJob>>();
            BuildJobService = new BuildJobService(
                [
                    new HangfireBuildJobRunner(
                        Substitute.For<IBackgroundJobClient>(),
                        [new NmtHangfireBuildJobFactory()]
                    ),
                    new ClearMLBuildJobRunner(
                        ClearMLService,
                        [
                            new NmtClearMLBuildJobFactory(
                                SharedFileService,
                                Substitute.For<ILanguageTagService>(),
                                Engines,
                                Options
                            )
                        ]
                    )
                ],
                Engines,
                new OptionsWrapper<BuildJobOptions>(new BuildJobOptions())
            );
            BuildJob = new NmtPreprocessBuildJob(
                PlatformService,
                Engines,
                LockFactory,
                Logger,
                BuildJobService,
                SharedFileService,
                CorpusService,
                new LanguageTagService()
            )
            {
                Seed = 1234
            };
        }

        public Task RunBuildJobAsync(Corpus corpus, bool useKeyTerms = true, string engineId = "engine1")
        {
            return BuildJob.RunAsync(
                engineId,
                "build1",
                [corpus],
                useKeyTerms ? null : "{\"use_key_terms\":false}",
                default
            );
        }

        public async Task<(int Source1Count, int Source2Count, int TargetCount, int TermCount)> GetTrainCountAsync()
        {
            using StreamReader srcReader = new(await SharedFileService.OpenReadAsync("builds/build1/train.src.txt"));
            using StreamReader trgReader = new(await SharedFileService.OpenReadAsync("builds/build1/train.trg.txt"));
            int src1Count = 0;
            int src2Count = 0;
            int trgCount = 0;
            int termCount = 0;
            string? srcLine;
            string? trgLine;
            while (
                (srcLine = await srcReader.ReadLineAsync()) is not null
                && (trgLine = await trgReader.ReadLineAsync()) is not null
            )
            {
                srcLine = srcLine.Trim();
                trgLine = trgLine.Trim();
                if (srcLine.StartsWith("Source one"))
                    src1Count++;
                else if (srcLine.StartsWith("Source two"))
                    src2Count++;
                else if (srcLine.Length == 0)
                    trgCount++;
                else
                    termCount++;
            }
            return (src1Count, src2Count, trgCount, termCount);
        }

        public async Task<int> GetPretranslateCountAsync()
        {
            using StreamReader reader =
                new(await SharedFileService.OpenReadAsync("builds/build1/pretranslate.src.json"));
            JsonArray? pretranslationJsonObject = JsonSerializer.Deserialize<JsonArray>(await reader.ReadToEndAsync());
            return pretranslationJsonObject?.Count ?? 0;
        }

        private void ZipParatextProject(string name)
        {
            ZipFile.CreateFromDirectory(Path.Combine(TestDataPath, name), Path.Combine(_tempDir.Path, $"{name}.zip"));
        }

        private CorpusFile ParatextFile(string name)
        {
            return new()
            {
                TextId = name,
                Format = FileFormat.Paratext,
                Location = Path.Combine(_tempDir.Path, $"{name}.zip")
            };
        }

        private static CorpusFile TextFile(string name)
        {
            return new()
            {
                TextId = "textId1",
                Format = FileFormat.Text,
                Location = Path.Combine(TestDataPath, $"{name}.txt")
            };
        }

        protected override void DisposeManagedResources()
        {
            _tempDir.Dispose();
        }
    }
}
