namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class ModelCleanupServiceTests
{
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

    [Test]
    public async Task CheckModelsAsync_ValidFiles()
    {
        TestEnvironment env = new();
        await env.CreateFilesAsync();

        Assert.That(
            await env.SharedFileService.ListFilesAsync("models"),
            Is.EquivalentTo(ValidFiles.Concat(InvalidFiles))
        );
        await env.CheckModelsAsync();
        // only valid files exist after running service
        Assert.That(await env.SharedFileService.ListFilesAsync("models"), Is.EquivalentTo(ValidFiles));
    }

    private class TestEnvironment
    {
        private readonly MemoryRepository<TranslationEngine> _engines;

        public TestEnvironment()
        {
            _engines = new MemoryRepository<TranslationEngine>();
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

            SharedFileService = new SharedFileService(Substitute.For<ILoggerFactory>());

            Service = new ModelCleanupService(
                Substitute.For<IServiceProvider>(),
                SharedFileService,
                Substitute.For<ILogger<ModelCleanupService>>()
            );
        }

        public ModelCleanupService Service { get; }
        public ISharedFileService SharedFileService { get; }

        public async Task CreateFilesAsync()
        {
            foreach (string path in ValidFiles)
            {
                await WriteFileStubAsync(path, "content");
            }
            foreach (string path in InvalidFiles)
            {
                await WriteFileStubAsync(path, "content");
            }
        }

        public Task CheckModelsAsync()
        {
            return Service.CheckModelsAsync(_engines, CancellationToken.None);
        }

        private async Task WriteFileStubAsync(string path, string content)
        {
            using StreamWriter streamWriter = new(await SharedFileService.OpenWriteAsync(path, CancellationToken.None));
            await streamWriter.WriteAsync(content);
        }
    }
}
