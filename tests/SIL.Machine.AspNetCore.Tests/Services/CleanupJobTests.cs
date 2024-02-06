namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class CleanupJobTests
{
    private readonly ISharedFileService _sharedFileService = new SharedFileService(Substitute.For<ILoggerFactory>());
    private readonly MemoryRepository<TranslationEngine> _engines = new MemoryRepository<TranslationEngine>();
    private readonly InMemoryStorage _memoryStorage = new();
    private static readonly List<string> validFiles = ["models/engineId1_1.tar.gz", "models/engineId2_2.tar.gz"];
    private static readonly List<string> invalidFiles =
    [
        "models/engineId2_1.targ.gz", // old build number
        "models/worngId_1.tar.gz",
        "models/engineId1_badbuildnumber.tar.gz",
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
        foreach (string path in validFiles)
        {
            await WriteFileStub(path, "content");
        }
        foreach (string path in invalidFiles)
        {
            await WriteFileStub(path, "content");
        }
    }

    [Test]
    public async Task RunAsync_ValidFiles()
    {
        await SetUpAsync();
        var cleanupJob = new CleanupOldModelsJob(
            _sharedFileService,
            _engines,
            Substitute.For<ILogger<CleanupOldModelsJob>>()
        );
        await cleanupJob.RunAsync();
        // both valid and invalid files still exist after running once
        Assert.That(
            _sharedFileService.ListFilesAsync("models").Result.ToHashSet(),
            Is.EquivalentTo(validFiles.Concat(invalidFiles).ToHashSet())
        );
        await cleanupJob.RunAsync();
        // only valid files exist after running twice
        Assert.That(
            _sharedFileService.ListFilesAsync("models").Result.ToHashSet(),
            Is.EquivalentTo(validFiles.ToHashSet())
        );
    }
}
