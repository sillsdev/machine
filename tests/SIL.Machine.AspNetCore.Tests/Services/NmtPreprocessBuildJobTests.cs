using System.Text.Json.Nodes;

namespace SIL.Machine.AspNetCore.Services
{
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
            using var env = new TestEnvironment();
            var corpus1 = new Corpus
            {
                Id = "corpusId1",
                SourceLanguage = "es",
                TargetLanguage = "en",
                PretranslateAll = pTAll,
                TrainOnAll = tOAll,
                PretranslateTextIds = pTTextIds is null ? new HashSet<string>() : pTTextIds.ToHashSet(),
                TrainOnTextIds = tOTextIds is null ? new HashSet<string>() : tOTextIds.ToHashSet(),
                SourceFiles = new List<CorpusFile>
                {
                    new CorpusFile
                    {
                        TextId = "textId1",
                        Format = FileFormat.Text,
                        Location = "../../../Services/data/source1.txt"
                    }
                },
                TargetFiles = new List<CorpusFile>
                {
                    new CorpusFile
                    {
                        TextId = "textId1",
                        Format = FileFormat.Text,
                        Location = "../../../Services/data/target1.txt"
                    }
                }
            };
            var corpora = new ReadOnlyList<Corpus>(new List<Corpus> { corpus1 });
            await env.BuildJob.RunAsync("engine1", "build1", corpora, null, default);
            using (var stream = await env.SharedFileService.OpenReadAsync("builds/build1/train.src.txt"))
            {
                using (var reader = new StreamReader(stream))
                {
                    //Split yields one more segment that there are new lines; thus, the "- 1"
                    Assert.That(reader.ReadToEnd().Split("\n").Length - 1, Is.EqualTo(numLinesWrittenToTrain));
                }
            }
            using (var stream = await env.SharedFileService.OpenReadAsync("builds/build1/pretranslate.src.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                    JsonArray? pretranslationJsonObject = JsonSerializer.Deserialize<JsonArray>(reader.ReadToEnd());
                    Assert.NotNull(pretranslationJsonObject);
                    Assert.That(pretranslationJsonObject!.ToList().Count, Is.EqualTo(numEntriesWrittenToPretranslate));
                }
            }
        }

        private class TestEnvironment : DisposableBase
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
                Engines = new MemoryRepository<TranslationEngine>();
                Engines.Add(
                    new TranslationEngine
                    {
                        Id = "engine1",
                        EngineId = "engine1",
                        SourceLanguage = "es",
                        TargetLanguage = "en",
                        BuildRevision = 1,
                        CurrentBuild = new Build { BuildId = "build1", JobState = BuildJobState.Pending }
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
                    CorpusService
                );
            }
        }
    }
}
