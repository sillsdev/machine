namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class LanguageTagServiceTests
{
    private readonly LanguageTagService _languageTagService;

    public LanguageTagServiceTests()
    {
        if (!Sldr.IsInitialized)
            Sldr.Initialize();
        _languageTagService = new LanguageTagService();
    }

    [Test]
    public void ConvertToFlores200Code_Iso639_1Code()
    {
        string code = _languageTagService.ConvertToFlores200Code("es");
        Assert.That(code, Is.EqualTo("spa_Latn"));
    }

    [Test]
    public void ConvertToFlores200Code_Iso639_3Code()
    {
        string code = _languageTagService.ConvertToFlores200Code("hne");
        Assert.That(code, Is.EqualTo("hne_Deva"));
    }

    [Test]
    public void ConvertToFlores200Code_ScriptCode()
    {
        string code = _languageTagService.ConvertToFlores200Code("ks-Arab");
        Assert.That(code, Is.EqualTo("kas_Arab"));
    }

    [Test]
    public void ConvertToFlores200Code_InvalidLangTag()
    {
        string code = _languageTagService.ConvertToFlores200Code("srp_Cyrl");
        Assert.That(code, Is.EqualTo("srp_Cyrl"));
    }

    [Test]
    public void ConvertToFlores200Code_ChineseNoScript()
    {
        string code = _languageTagService.ConvertToFlores200Code("zh");
        Assert.That(code, Is.EqualTo("zho_Hans"));
    }

    [Test]
    public void ConvertToFlores200Code_ChineseScript()
    {
        string code = _languageTagService.ConvertToFlores200Code("zh-Hant");
        Assert.That(code, Is.EqualTo("zho_Hant"));
    }

    [Test]
    public void ConvertToFlores200Code_ChineseRegion()
    {
        string code = _languageTagService.ConvertToFlores200Code("zh-TW");
        Assert.That(code, Is.EqualTo("zho_Hant"));
    }

    [Test]
    public void ConvertToFlores200Code_MandarinChineseNoScript()
    {
        string code = _languageTagService.ConvertToFlores200Code("cmn");
        Assert.That(code, Is.EqualTo("zho_Hans"));
    }

    [Test]
    public void ConvertToFlores200Code_MandarinChineseScript()
    {
        string code = _languageTagService.ConvertToFlores200Code("cmn-Hant");
        Assert.That(code, Is.EqualTo("zho_Hant"));
    }

    [Test]
    public void ConvertToFlores200Code_Macrolanguage()
    {
        string code = _languageTagService.ConvertToFlores200Code("ms");
        Assert.That(code, Is.EqualTo("zsm_Latn"));
    }

    [Test]
    public void ConvertToFlores200Code_Arabic()
    {
        string code = _languageTagService.ConvertToFlores200Code("arb");
        Assert.That(code, Is.EqualTo("arb_Arab"));
    }

    [Test]
    public void ConvertToFlores200Code_HandleISO639_3_InsteadOfISO639_1()
    {
        string code = _languageTagService.ConvertToFlores200Code("eng");
        Assert.That(code, Is.EqualTo("eng_Latn"));
    }

    [Test]
    public void ConvertToFlores200Code_DashToUnderscore()
    {
        string code = _languageTagService.ConvertToFlores200Code("eng-Latn");
        Assert.That(code, Is.EqualTo("eng_Latn"));
    }
}
