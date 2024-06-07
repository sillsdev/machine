namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class ModelCleanupServiceTests
{
    private readonly ISharedFileService _sharedFileService = new SharedFileService(Substitute.For<ILoggerFactory>());
    private readonly MemoryRepository<TranslationEngine> _engines = new MemoryRepository<TranslationEngine>();
    private static readonly List<string> ValidFiles =
    [
        "models/engineId1_1.tar.gz",
        "models/engineId2_2.tar.gz",
        "models/engineId2_3.tar.gz" // only one build ahead - keep
    ];
    private static readonly List<string> InvalidFiles =
    [
        "models/engineId2_1.tar.gz", // 1 build behind
        "models/engineId2_4.tar.gz", // 2 builds ahead
        "models/wrongId_1.tar.gz",
        "models/engineId1_badBuildNumber.tar.gz",
        "models/noBuildNumber.tar.gz",
        "models/engineId1_1.differentExtension"
    ];

    private async Task SetUpAsync()
    {
        _engines.Add(
            new TranslationEngine
            {
                Id = "engine1",
                EngineId = "engineId1",
                Type = TranslationEngineType.Nmt,
                SourceLanguage = "es",
                TargetLanguage = "en",
                BuildRevision = 1,
                IsModelPersisted = true
            }
        );
        _engines.Add(
            new TranslationEngine
            {
                Id = "engine2",
                EngineId = "engineId2",
                Type = TranslationEngineType.Nmt,
                SourceLanguage = "es",
                TargetLanguage = "en",
                BuildRevision = 2,
                IsModelPersisted = true
            }
        );
        async Task WriteFileStub(string path, string content)
        {
            using StreamWriter streamWriter =
                new(await _sharedFileService.OpenWriteAsync(path, CancellationToken.None));
            await streamWriter.WriteAsync(content);
        }
        foreach (string path in ValidFiles)
        {
            await WriteFileStub(path, "content");
        }
        foreach (string path in InvalidFiles)
        {
            await WriteFileStub(path, "content");
        }
    }

    public class TestModelCleanupService(
        IServiceProvider serviceProvider,
        ISharedFileService sharedFileService,
        IRepository<TranslationEngine> engines,
        ILogger<ModelCleanupService> logger
    ) : ModelCleanupService(serviceProvider, sharedFileService, engines, logger)
    {
        public async Task DoWorkAsync() =>
            await base.DoWorkAsync(Substitute.For<IServiceScope>(), CancellationToken.None);
    }

    [Test]
    public async Task DoWorkAsync_ValidFiles()
    {
        await SetUpAsync();

        var cleanupJob = new TestModelCleanupService(
            Substitute.For<IServiceProvider>(),
            _sharedFileService,
            _engines,
            Substitute.For<ILogger<ModelCleanupService>>()
        );
        Assert.That(
            _sharedFileService.ListFilesAsync("models").Result.ToHashSet(),
            Is.EquivalentTo(ValidFiles.Concat(InvalidFiles).ToHashSet())
        );
        await cleanupJob.DoWorkAsync();
        // only valid files exist after running service
        Assert.That(
            _sharedFileService.ListFilesAsync("models").Result.ToHashSet(),
            Is.EquivalentTo(ValidFiles.ToHashSet())
        );
    }
}
