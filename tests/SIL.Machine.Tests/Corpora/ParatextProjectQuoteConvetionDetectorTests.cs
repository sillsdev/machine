using System.Text;
using NUnit.Framework;
using SIL.Machine.PunctuationAnalysis;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ParatextProjectQuoteConventionDetectorTests
{
    private static readonly QuoteConvention StandardEnglishQuoteConvention =
        QuoteConventions.Standard.GetQuoteConventionByName("standard_english");
    private static readonly QuoteConvention StandardFrenchQuoteConvention =
        QuoteConventions.Standard.GetQuoteConventionByName("standard_french");

    [Test]
    public void TestGetQuotationAnalysis()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "41MATTest.SFM",
                    $@"\id MAT
{GetTestChapter(1, StandardEnglishQuoteConvention)}
"
                }
            }
        );
        QuoteConventionAnalysis analysis = env.GetQuoteConvention();
        Assert.That(analysis, Is.Not.Null);
        Assert.That(analysis.BestQuoteConventionScore, Is.GreaterThan(0.8));
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_english"));
    }

    [Test]
    public void TestGetQuotationByBook()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "41MATTest.SFM",
                    $@"\id MAT
{GetTestChapter(1, StandardEnglishQuoteConvention)}
"
                },
                {
                    "42MRKTest.SFM",
                    $@"\id MRK
{GetTestChapter(1, StandardFrenchQuoteConvention)}
"
                }
            }
        );
        QuoteConventionAnalysis analysis = env.GetQuoteConvention("MRK");
        Assert.That(analysis, Is.Not.Null);
        Assert.That(analysis.BestQuoteConventionScore, Is.GreaterThan(0.8));
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_french"));
    }

    [Test]
    public void TestGetQuotationConventionByChapter()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "41MATTest.SFM",
                    $@"\id MAT
{GetTestChapter(1, StandardEnglishQuoteConvention)}
"
                },
                {
                    "42MRKTest.SFM",
                    $@"\id MRK
{GetTestChapter(1, StandardEnglishQuoteConvention)}
{GetTestChapter(2, StandardFrenchQuoteConvention)}
{GetTestChapter(3, StandardEnglishQuoteConvention)}
{GetTestChapter(4, StandardEnglishQuoteConvention)}
{GetTestChapter(5, StandardFrenchQuoteConvention)}
"
                }
            }
        );
        QuoteConventionAnalysis analysis = env.GetQuoteConvention("MRK2,4-5");
        Assert.That(analysis, Is.Not.Null);
        Assert.That(analysis.BestQuoteConventionScore, Is.GreaterThan(0.66));
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_french"));
    }

    [Test]
    public void TestGetQuotationConventionByChapterIndeterminate()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "41MATTest.SFM",
                    $@"\id MAT
{GetTestChapter(1)}
{GetTestChapter(2, StandardEnglishQuoteConvention)}
{GetTestChapter(3)}
"
                }
            }
        );
        QuoteConventionAnalysis analysis = env.GetQuoteConvention("MAT1,3");
        Assert.That(analysis, Is.Null);
    }

    [Test]
    public void TestGetQuotationConventionInvalidBookCode()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "41MATTest.SFM",
                    $@"\id LUK
{GetTestChapter(1, StandardEnglishQuoteConvention)}
"
                }
            }
        );
        QuoteConventionAnalysis analysis = env.GetQuoteConvention("MAT");
        Assert.That(analysis, Is.Null);
    }

    private class TestEnvironment(ParatextProjectSettings? settings = null, Dictionary<string, string>? files = null)
    {
        public ParatextProjectQuoteConventionDetector Detector { get; } =
            new MemoryParatextProjectQuoteConventionDetector(
                settings ?? new DefaultParatextProjectSettings(),
                files ?? new()
            );

        public QuoteConventionAnalysis GetQuoteConvention(string? scriptureRange = null)
        {
            Dictionary<int, List<int>>? chapters = null;
            if (scriptureRange != null)
            {
                chapters = ScriptureRangeParser
                    .GetChapters(scriptureRange)
                    .ToDictionary(kvp => Canon.BookIdToNumber(kvp.Key), kvp => kvp.Value);
            }
            return Detector.GetQuoteConventionAnalysis(includeChapters: chapters);
        }
    }

    private static string GetTestChapter(int number, QuoteConvention? quoteConvention = null)
    {
        string leftQuote = quoteConvention != null ? quoteConvention.GetOpeningQuotationMarkAtDepth(1) : "";
        string rightQuote = quoteConvention != null ? quoteConvention.GetClosingQuotationMarkAtDepth(1) : "";
        return $@"\c {number}
\v 1 Someone said, {leftQuote}This is something I am saying!
\v 2 This is also something I am saying{rightQuote} (that is, {leftQuote}something I am speaking{rightQuote}).
\p
\v 3 Other text, and someone else said,
\q1
\v 4 {leftQuote}Things
\q2 someone else said!
\q3 and more things someone else said.{rightQuote}
\m That is why he said {leftQuote}things someone else said.{rightQuote}
\v 5 Then someone said, {leftQuote}More things someone said.{rightQuote}
        ";
    }

    private class DefaultParatextProjectSettings(
        string name = "Test",
        string fullName = "TestProject",
        Encoding? encoding = null,
        ScrVers? versification = null,
        UsfmStylesheet? stylesheet = null,
        string fileNamePrefix = "",
        string fileNameForm = "41MAT",
        string fileNameSuffix = "Test.SFM",
        string biblicalTermsListType = "Project",
        string biblicalTermsProjectName = "Test",
        string biblicalTermsFileName = "ProjectBiblicalTerms.xml",
        string languageCode = "en"
    )
        : ParatextProjectSettings(
            name,
            fullName,
            encoding ?? Encoding.UTF8,
            versification ?? ScrVers.English,
            stylesheet ?? new UsfmStylesheet("usfm.sty"),
            fileNamePrefix,
            fileNameForm,
            fileNameSuffix,
            biblicalTermsListType,
            biblicalTermsProjectName,
            biblicalTermsFileName,
            languageCode
        ) { }
}
