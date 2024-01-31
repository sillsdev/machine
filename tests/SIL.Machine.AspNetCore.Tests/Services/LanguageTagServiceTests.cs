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
    [TestCase("es", "spa_Latn", Description = "Iso639_1Code")]
    [TestCase("hne", "hne_Deva", Description = "Iso639_3Code")]
    [TestCase("ks-Arab", "kas_Arab", Description = "ScriptCode")]
    [TestCase("srp_Cyrl", "srp_Cyrl", Description = "InvalidLangTag")]
    [TestCase("zh", "zho_Hans", Description = "ChineseNoScript")]
    [TestCase("zh-Hant", "zho_Hant", Description = "ChineseScript")]
    [TestCase("zh-TW", "zho_Hant", Description = "ChineseRegion")]
    [TestCase("cmn", "zho_Hans", Description = "MandarinChineseNoScript")]
    [TestCase("cmn-Hant", "zho_Hant", Description = "MandarinChineseScript")]
    [TestCase("ms", "zsm_Latn", Description = "Macrolanguage")]
    [TestCase("arb", "arb_Arab", Description = "Arabic")]
    [TestCase("eng", "eng_Latn", Description = "InsteadOfISO639_1")]
    [TestCase("eng-Latn", "eng_Latn", Description = "DashToUnderscore")]
    [TestCase("kor", "kor_Hang", Description = "KoreanScript")]
    [TestCase("kor_Kore", "kor_Hang", Description = "KoreanScriptCorrection")]
    public void ConvertToFlores200CodeTest(string language, string internalCodeTruth)
    {
        _languageTagService.ConvertToFlores200Code(language, out string internalCode);
        Assert.That(internalCode, Is.EqualTo(internalCodeTruth));
    }

    [Test]
    [TestCase("en", "eng_Latn", true)]
    [TestCase("ms", "zsm_Latn", true)]
    [TestCase("cmn", "zho_Hans", true)]
    [TestCase("xyz", "xyz", false)]
    public void GetLanguageInfoAsync(string languageCode, string? resolvedLanguageCode, bool nativeLanguageSupport)
    {
        bool isNative = _languageTagService.ConvertToFlores200Code(languageCode, out string internalCode);
        Assert.Multiple(() =>
        {
            Assert.That(internalCode, Is.EqualTo(resolvedLanguageCode));
            Assert.That(isNative, Is.EqualTo(nativeLanguageSupport));
        });
    }
}
