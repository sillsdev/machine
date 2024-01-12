namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class NmtClearMLBuildJobFactoryTests
{
    [Test]
    public async Task CreateJobScriptAsync_BuildOptions()
    {
        var env = new TestEnvironment();
        string script = await env.BuildJobFactory.CreateJobScriptAsync(
            "engine1",
            "build1",
            NmtBuildStages.Train,
            buildOptions: "{ \"max_steps\": \"10\" }"
        );
        Assert.That(
            script,
            Is.EqualTo(
                @"from machine.jobs.build_nmt_engine import run
args = {
    'model_type': 'test_model',
    'engine_id': 'engine1',
    'build_id': 'build1',
    'src_lang': 'spa_Latn',
    'trg_lang': 'eng_Latn',
    'shared_file_uri': 's3://bucket',
    'shared_file_folder': 'folder1/folder2',
    'build_options': '''{ ""max_steps"": ""10"" }''',
    'clearml': True
}
run(args)
".ReplaceLineEndings("\n")
            )
        );
    }

    [Test]
    public async Task CreateJobScriptAsync_NoBuildOptions()
    {
        var env = new TestEnvironment();
        string script = await env.BuildJobFactory.CreateJobScriptAsync("engine1", "build1", NmtBuildStages.Train);
        Assert.That(
            script,
            Is.EqualTo(
                @"from machine.jobs.build_nmt_engine import run
args = {
    'model_type': 'test_model',
    'engine_id': 'engine1',
    'build_id': 'build1',
    'src_lang': 'spa_Latn',
    'trg_lang': 'eng_Latn',
    'shared_file_uri': 's3://bucket',
    'shared_file_folder': 'folder1/folder2',
    'clearml': True
}
run(args)
".ReplaceLineEndings("\n")
            )
        );
    }

    private class TestEnvironment
    {
        public ISharedFileService SharedFileService { get; }
        public MemoryRepository<TranslationEngine> Engines { get; }
        public IOptionsMonitor<ClearMLOptions> Options { get; }
        public ILanguageTagService LanguageTagService { get; }
        public NmtClearMLBuildJobFactory BuildJobFactory { get; }

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
            Options = Substitute.For<IOptionsMonitor<ClearMLOptions>>();
            Options.CurrentValue.Returns(new ClearMLOptions { ModelType = "test_model" });
            SharedFileService = Substitute.For<ISharedFileService>();
            SharedFileService.GetBaseUri().Returns(new Uri("s3://bucket/folder1/folder2"));
            LanguageTagService = Substitute.For<ILanguageTagService>();
            LanguageTagService.ConvertToFlores200Code("es").Returns("spa_Latn");
            LanguageTagService.ConvertToFlores200Code("en").Returns("eng_Latn");
            BuildJobFactory = new NmtClearMLBuildJobFactory(SharedFileService, LanguageTagService, Engines, Options);
        }
    }
}
