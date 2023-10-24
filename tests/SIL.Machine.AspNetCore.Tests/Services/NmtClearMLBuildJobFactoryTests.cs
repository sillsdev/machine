namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class NmtClearMLBuildJobFactoryTests
{
    [Test]
    public async Task CreateJobScriptAsync_Iso639_1Code()
    {
        var env = new TestEnvironment();
        env.AddEngine("es");
        string script = await env.Factory.CreateJobScriptAsync("engine1", "build1", "train");
        Assert.That(script, Contains.Substring("'trg_lang': 'spa_Latn'"));
    }

    [Test]
    public async Task CreateJobScriptAsync_Iso639_3Code()
    {
        var env = new TestEnvironment();
        env.AddEngine("hne");
        string script = await env.Factory.CreateJobScriptAsync("engine1", "build1", "train");
        Assert.That(script, Contains.Substring("'trg_lang': 'hne_Deva'"));
    }

    [Test]
    public async Task CreateJobScriptAsync_ScriptCode()
    {
        var env = new TestEnvironment();
        env.AddEngine("ks-Arab");
        string script = await env.Factory.CreateJobScriptAsync("engine1", "build1", "train");
        Assert.That(script, Contains.Substring("'trg_lang': 'kas_Arab'"));
    }

    [Test]
    public async Task CreateJobScriptAsync_InvalidLangTag()
    {
        var env = new TestEnvironment();
        env.AddEngine("srp_Cyrl");
        string script = await env.Factory.CreateJobScriptAsync("engine1", "build1", "train");
        Assert.That(script, Contains.Substring("'trg_lang': 'srp_Cyrl'"));
    }

    [Test]
    public async Task CreateJobScriptAsync_ChineseNoScript()
    {
        var env = new TestEnvironment();
        env.AddEngine("zh");
        string script = await env.Factory.CreateJobScriptAsync("engine1", "build1", "train");
        Assert.That(script, Contains.Substring("'trg_lang': 'zho_Hans'"));
    }

    [Test]
    public async Task CreateJobScriptAsync_ChineseScript()
    {
        var env = new TestEnvironment();
        env.AddEngine("zh-Hant");
        string script = await env.Factory.CreateJobScriptAsync("engine1", "build1", "train");
        Assert.That(script, Contains.Substring("'trg_lang': 'zho_Hant'"));
    }

    [Test]
    public async Task CreateJobScriptAsync_ChineseRegion()
    {
        var env = new TestEnvironment();
        env.AddEngine("zh-TW");
        string script = await env.Factory.CreateJobScriptAsync("engine1", "build1", "train");
        Assert.That(script, Contains.Substring("'trg_lang': 'zho_Hant'"));
    }

    [Test]
    public async Task CreateJobScriptAsync_MandarinChineseNoScript()
    {
        var env = new TestEnvironment();
        env.AddEngine("cmn");
        string script = await env.Factory.CreateJobScriptAsync("engine1", "build1", "train");
        Assert.That(script, Contains.Substring("'trg_lang': 'zho_Hans'"));
    }

    [Test]
    public async Task CreateJobScriptAsync_MandarinChineseScript()
    {
        var env = new TestEnvironment();
        env.AddEngine("cmn-Hant");
        string script = await env.Factory.CreateJobScriptAsync("engine1", "build1", "train");
        Assert.That(script, Contains.Substring("'trg_lang': 'zho_Hant'"));
    }

    [Test]
    public async Task CreateJobScriptAsync_Macrolanguage()
    {
        var env = new TestEnvironment();
        env.AddEngine("ms");
        string script = await env.Factory.CreateJobScriptAsync("engine1", "build1", "train");
        Assert.That(script, Contains.Substring("'trg_lang': 'zsm_Latn'"));
    }

    private class TestEnvironment
    {
        public TestEnvironment()
        {
            if (!Sldr.IsInitialized)
                Sldr.Initialize(offlineMode: true);

            Engines = new MemoryRepository<TranslationEngine>();

            SharedFileService = new SharedFileService(Substitute.For<ILoggerFactory>());
            var clearMLOptions = Substitute.For<IOptionsMonitor<ClearMLOptions>>();
            clearMLOptions.CurrentValue.Returns(new ClearMLOptions());
            Factory = new NmtClearMLBuildJobFactory(SharedFileService, Engines, clearMLOptions);
        }

        public NmtClearMLBuildJobFactory Factory { get; }
        public MemoryRepository<TranslationEngine> Engines { get; }
        public ISharedFileService SharedFileService { get; }

        public void AddEngine(string targetLanguage)
        {
            Engines.Add(
                new TranslationEngine
                {
                    Id = "engine1",
                    EngineId = "engine1",
                    SourceLanguage = "en",
                    TargetLanguage = targetLanguage,
                    BuildRevision = 1
                }
            );
        }
    }
}
