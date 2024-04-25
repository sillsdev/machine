namespace SIL.Machine.AspNetCore.Services;

[TestFixture]
public class LanguageTagServiceTests
{
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
        if (!Sldr.IsInitialized)
            Sldr.Initialize();
        new LanguageTagService().ConvertToFlores200Code(language, out string internalCode);
        Assert.That(internalCode, Is.EqualTo(internalCodeTruth));
    }

    [Test]
    [TestCase("en", "eng_Latn", true)]
    [TestCase("ms", "zsm_Latn", true)]
    [TestCase("cmn", "zho_Hans", true)]
    [TestCase("xyz", "xyz", false)]
    public void GetLanguageInfoAsync(string languageCode, string? resolvedLanguageCode, bool nativeLanguageSupport)
    {
        if (!Sldr.IsInitialized)
            Sldr.Initialize();
        bool isNative = new LanguageTagService().ConvertToFlores200Code(languageCode, out string internalCode);
        Assert.Multiple(() =>
        {
            Assert.That(internalCode, Is.EqualTo(resolvedLanguageCode));
            Assert.That(isNative, Is.EqualTo(nativeLanguageSupport));
        });
    }

    public class TestLanguageTagService : LanguageTagService
    {
        // Don't call Sldr initialize to call
        protected override void InitializeSldrLanguageTags()
        {
            // remove langtags.json to force download
            var cachedAllTagsPath = Path.Combine(Sldr.SldrCachePath, "langtags.json");
            if (File.Exists(cachedAllTagsPath))
                File.Delete(cachedAllTagsPath);
            Directory.CreateDirectory(Sldr.SldrCachePath);
        }
    }

    [Test]
    public void BackupLangtagsJsonTest()
    {
        if (!Sldr.IsInitialized)
            Sldr.Initialize();
        var service = new TestLanguageTagService();
        service.ConvertToFlores200Code("en", out string internalCode);
        Assert.That(internalCode, Is.EqualTo("eng_Latn"));
    }
}
