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
    [TestCase("es", "spa_Latn")]
    [TestCase("hne", "hne_Deva")]
    [TestCase("ks-Arab", "kas_Arab")]
    [TestCase("srp_Cyrl", "srp_Cyrl")]
    [TestCase("zh", "zho_Hans")]
    [TestCase("zh-Hant", "zho_Hant")]
    [TestCase("zh-TW", "zho_Hant")]
    [TestCase("cmn", "zho_Hans")]
    [TestCase("cmn-Hant", "zho_Hant")]
    [TestCase("ms", "zsm_Latn")]
    [TestCase("arb", "arb_Arab")]
    [TestCase("eng", "eng_Latn")]
    [TestCase("eng_Latn", "eng_Latn")]
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
