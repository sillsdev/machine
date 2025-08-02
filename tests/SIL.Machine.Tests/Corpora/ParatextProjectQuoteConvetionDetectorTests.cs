using System.Text;
using NUnit.Framework;
using SIL.Machine.Corpora.PunctuationAnalysis;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ParatextProjectQuoteConventionDetectorTests
{
    [Test]
    public void TestGetQuotationAnalysis()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "41MATTest.SFM",
                    @"\id MAT
\c 1
\v 1 Someone said, “This is something I am saying!
\v 2 This is also something I am saying” (that is, “something I am speaking”).
\p
\v 3 Other text, and someone else said,
\q1
\v 4 “Things
\q2 someone else said!
\q3 and more things someone else said.”
\m That is why he said “things someone else said.”
\v 5 Then someone said, “More things someone said.”"
                }
            }
        );
        QuoteConventionAnalysis analysis = env.GetQuoteConvention();
        Assert.That(analysis, Is.Not.Null);
        Assert.That(analysis.BestQuoteConventionScore, Is.GreaterThan(0.8));
        Assert.That(analysis.BestQuoteConvention.Name, Is.EqualTo("standard_english"));
    }

    private class TestEnvironment(ParatextProjectSettings? settings = null, Dictionary<string, string>? files = null)
    {
        public ParatextProjectQuoteConventionDetector Detector { get; } =
            new MemoryParatextProjectQuoteConventionDetector(
                settings ?? new DefaultParatextProjectSettings(),
                files ?? new()
            );

        public QuoteConventionAnalysis GetQuoteConvention()
        {
            return Detector.GetQuoteConventionAnalysis();
        }
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
