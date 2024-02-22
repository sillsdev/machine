namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class NmtPreprocessBuildJobTests
{
    [Test]
    [TestCase(false, false, null, null, 0, 0)]
    [TestCase(false, true, null, null, 5, 0)]
    [TestCase(false, false, new string[] { "textId1" }, null, 0, 2)]
    [TestCase(false, false, null, new string[] { "textId1" }, 5, 0)]
    [TestCase(true, true, null, null, 5, 2)]
    [TestCase(true, false, null, null, 0, 2)]
    public async Task BuildJobTest(
        bool pTAll,
        bool tOAll,
        IReadOnlyList<string>? pTTextIds,
        IReadOnlyList<string>? tOTextIds,
        int numLinesWrittenToTrain,
        int numEntriesWrittenToPretranslate
    )
    {
        using TestEnvironment env = new();
        var corpus1 = new Corpus
        {
            Id = "corpusId1",
            SourceLanguage = "es",
            TargetLanguage = "en",
            PretranslateAll = pTAll,
            TrainOnAll = tOAll,
            PretranslateTextIds = pTTextIds?.ToHashSet() ?? [],
            TrainOnTextIds = tOTextIds?.ToHashSet() ?? [],
            SourceFiles =
            [
                new()
                {
                    TextId = "textId1",
                    Format = FileFormat.Text,
                    Location = Path.Combine("..", "..", "..", "Services", "data", "source1.txt")
                }
            ],
            TargetFiles =
            [
                new()
                {
                    TextId = "textId1",
                    Format = FileFormat.Text,
                    Location = Path.Combine("..", "..", "..", "Services", "data", "target1.txt")
                }
            ]
        };
        await env.BuildJob.RunAsync("engine1", "build1", [corpus1], null, default);
        using (StreamReader reader = new(await env.SharedFileService.OpenReadAsync("builds/build1/train.src.txt")))
        {
            //Split yields one more segment that there are new lines; thus, the "- 1"
            Assert.That(reader.ReadToEnd().Split("\n").Length - 1, Is.EqualTo(numLinesWrittenToTrain));
        }

        using (
            StreamReader reader = new(await env.SharedFileService.OpenReadAsync("builds/build1/pretranslate.src.json"))
        )
        {
            JsonArray? pretranslationJsonObject = JsonSerializer.Deserialize<JsonArray>(reader.ReadToEnd());
            Assert.That(pretranslationJsonObject, Is.Not.Null);
            Assert.That(pretranslationJsonObject, Has.Count.EqualTo(numEntriesWrittenToPretranslate));
        }
    }

    [Test]
    [TestCase(null, 1, 0)]
    [TestCase("{\"use_key_terms\":false}", 0, 0)]
    public async Task BuildJobTest_Paratext(
        string? buildOptions,
        int numLinesWrittenToTrain,
        int numEntriesWrittenToPretranslate
    )
    {
        using TestEnvironment env = new();
        var corpus1 = new Corpus
        {
            Id = "corpusId1",
            SourceLanguage = "es",
            TargetLanguage = "en",
            PretranslateAll = false,
            TrainOnAll = false,
            PretranslateTextIds = new HashSet<string>(),
            TrainOnTextIds = new HashSet<string>(),
            SourceFiles =
            [
                new()
                {
                    TextId = "textId1",
                    Format = FileFormat.Paratext,
                    Location = Path.Combine(Path.GetTempPath(), "Project.zip")
                }
            ],
            TargetFiles =
            [
                new()
                {
                    TextId = "textId1",
                    Format = FileFormat.Paratext,
                    Location = Path.Combine(Path.GetTempPath(), "Project.zip")
                }
            ]
        };
        await env.BuildJob.RunAsync("engine1", "build1", [corpus1], buildOptions, default);
        using (StreamReader reader = new(await env.SharedFileService.OpenReadAsync("builds/build1/train.src.txt")))
        {
            //Split yields one more segment that there are new lines; thus, the "- 1"
            Assert.That(reader.ReadToEnd().Split("\n").Length - 1, Is.EqualTo(numLinesWrittenToTrain));
        }

        using (
            StreamReader reader = new(await env.SharedFileService.OpenReadAsync("builds/build1/pretranslate.src.json"))
        )
        {
            JsonArray? pretranslationJsonObject = JsonSerializer.Deserialize<JsonArray>(reader.ReadToEnd());
            Assert.That(pretranslationJsonObject, Is.Not.Null);
            Assert.That(pretranslationJsonObject, Has.Count.EqualTo(numEntriesWrittenToPretranslate));
        }
    }

    [Test]
    [TestCase("MAT", "1CH", 23, 4)]
    [TestCase("NT;LEV", "1CH", 25, 4)]
    [TestCase("OT", "MRK", 10, 0)]
    [TestCase("OT", "MLK", 0, 0, true)]
    public async Task BuildJobTest_Chapterlevel(
        string trainOnBiblicalRangeChapters,
        string pretranslateBiblicalRangeChapters,
        int numLinesWrittenToTrain,
        int numEntriesWrittenToPretranslate,
        bool throwsException = false
    )
    {
        using TestEnvironment env = new();
        var parser = new ScriptureRangeParser();

        Corpus corpus1;
        if (throwsException)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                corpus1 = new Corpus
                {
                    Id = "corpusId1",
                    SourceLanguage = "en",
                    TargetLanguage = "es",
                    PretranslateAll = false,
                    TrainOnAll = false,
                    PretranslateChapters = parser
                        .GetChapters(pretranslateBiblicalRangeChapters)
                        .ToDictionary(kvp => kvp.Key, kvp => (IReadOnlySet<int>)kvp.Value.ToHashSet()),
                    TrainOnChapters = parser
                        .GetChapters(trainOnBiblicalRangeChapters)
                        .ToDictionary(kvp => kvp.Key, kvp => (IReadOnlySet<int>)kvp.Value.ToHashSet()),
                    PretranslateTextIds = new HashSet<string>(),
                    TrainOnTextIds = new HashSet<string>(),
                    SourceFiles =
                    [
                        new()
                        {
                            TextId = "textId1",
                            Format = FileFormat.Paratext,
                            Location = Path.Combine(Path.GetTempPath(), "Project.zip")
                        }
                    ],
                    TargetFiles =
                    [
                        new()
                        {
                            TextId = "textId1",
                            Format = FileFormat.Paratext,
                            Location = Path.Combine(Path.GetTempPath(), "Project2.zip")
                        }
                    ]
                };
            });
            return;
        }
        else
        {
            corpus1 = new Corpus
            {
                Id = "corpusId1",
                SourceLanguage = "en",
                TargetLanguage = "es",
                PretranslateAll = false,
                TrainOnAll = false,
                PretranslateChapters = parser
                    .GetChapters(pretranslateBiblicalRangeChapters)
                    .ToDictionary(kvp => kvp.Key, kvp => (IReadOnlySet<int>)kvp.Value.ToHashSet()),
                TrainOnChapters = parser
                    .GetChapters(trainOnBiblicalRangeChapters)
                    .ToDictionary(kvp => kvp.Key, kvp => (IReadOnlySet<int>)kvp.Value.ToHashSet()),
                PretranslateTextIds = new HashSet<string>(),
                TrainOnTextIds = new HashSet<string>(),
                SourceFiles =
                [
                    new()
                    {
                        TextId = "textId1",
                        Format = FileFormat.Paratext,
                        Location = Path.Combine(Path.GetTempPath(), "Project.zip")
                    }
                ],
                TargetFiles =
                [
                    new()
                    {
                        TextId = "textId1",
                        Format = FileFormat.Paratext,
                        Location = Path.Combine(Path.GetTempPath(), "Project2.zip")
                    }
                ]
            };
        }
        await env.BuildJob.RunAsync("engine1", "build1", [corpus1], "{\"use_key_terms\":false}", default);
        using (StreamReader reader = new(await env.SharedFileService.OpenReadAsync("builds/build1/train.src.txt")))
        {
            //Split yields one more segment that there are new lines; thus, the "- 1"
            string text = reader.ReadToEnd();
            Assert.That(text.Split("\n").Length - 1, Is.EqualTo(numLinesWrittenToTrain), text);
        }

        using (Stream stream = await env.SharedFileService.OpenReadAsync("builds/build1/pretranslate.src.json"))
        using (StreamReader reader = new(stream))
        {
            JsonArray? pretranslationJsonObject = JsonSerializer.Deserialize<JsonArray>(reader.ReadToEnd());
            Assert.That(pretranslationJsonObject, Is.Not.Null);
            Assert.That(
                pretranslationJsonObject,
                Has.Count.EqualTo(numEntriesWrittenToPretranslate),
                JsonSerializer.Serialize(pretranslationJsonObject)
            );
        }
    }

    private class TestEnvironment : ObjectModel.DisposableBase
    {
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

        public TestEnvironment()
        {
            if (!Sldr.IsInitialized)
                Sldr.Initialize(offlineMode: true);

            CleanupProjectFiles();
            ZipFile.CreateFromDirectory(
                Path.Combine("..", "..", "..", "Services", "data", "paratext"),
                Path.Combine(Path.GetTempPath(), "Project.zip")
            );
            ZipFile.CreateFromDirectory(
                Path.Combine("..", "..", "..", "Services", "data", "paratext2"),
                Path.Combine(Path.GetTempPath(), "Project2.zip")
            );

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
                .CreateTaskAsync("build1", "project1", Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult("job1"));
            SharedFileService = new SharedFileService(Substitute.For<ILoggerFactory>());
            Logger = Substitute.For<ILogger<NmtPreprocessBuildJob>>();
            BuildJobService = new BuildJobService(
                new IBuildJobRunner[]
                {
                    new HangfireBuildJobRunner(
                        Substitute.For<IBackgroundJobClient>(),
                        new[] { new NmtHangfireBuildJobFactory() }
                    ),
                    new ClearMLBuildJobRunner(
                        ClearMLService,
                        new[]
                        {
                            new NmtClearMLBuildJobFactory(
                                SharedFileService,
                                Substitute.For<ILanguageTagService>(),
                                Engines,
                                Options
                            )
                        }
                    )
                },
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
            );
        }

        protected override void DisposeManagedResources()
        {
            CleanupProjectFiles();
        }

        private static void CleanupProjectFiles()
        {
            File.Delete(Path.Combine(Path.GetTempPath(), "Project.zip"));
            File.Delete(Path.Combine(Path.GetTempPath(), "Project2.zip"));
        }
    }
}
