using System.Text;
using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ParatextProjectTermsCorpusTests
{
    [Test]
    public void TestGetKeyTermsFromTermsRenderings()
    {
        var env = new TestEnvironment(
            files: new Dictionary<string, string>()
            {
                {
                    "ProjectBiblicalTerms.xml",
                    @"
<BiblicalTermsList>
  <Term Id=""אֲחַשְׁוֵרוֹשׁ"">
  <Category>PN</Category>
    <Gloss>Ahasuerus</Gloss>
  </Term>
</BiblicalTermsList>"
                },
                {
                    "TermRenderings.xml",
                    @"
<TermRenderingsList>
  <TermRendering Id=""אֲחַשְׁוֵרוֹשׁ"" Guess=""false"">
    <Renderings>Xerxes</Renderings>
    <Glossary />
    <Changes />
    <Notes />
    <Denials />
  </TermRendering>
</TermRenderingsList>"
                }
            }
        );
        IList<TextRow> rows = env.Corpus.GetRows().ToList();
        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(string.Join(" ", rows.First().Segment), Is.EqualTo("Xerxes"));
    }

    [Test]
    public void TestGetKeyTermsFromTermsLocalizations_NoTermRenderings()
    {
        var env = new TestEnvironment(
            new DefaultParatextProjectSettings(
                biblicalTermsListType: "Major",
                biblicalTermsFileName: "BiblicalTerms.xml"
            )
        );
        IList<TextRow> rows = env.Corpus.GetRows().ToList();
        Assert.That(rows.Count, Is.EqualTo(5726));
        Assert.That(string.Join(" ", rows.First().Segment), Is.EqualTo("Abagtha"));
    }

    [Test]
    public void TestGetKeyTermsFromTermsLocalizations_NoTermRenderings_PreferLocalization()
    {
        var env = new TestEnvironment(
            new DefaultParatextProjectSettings(
                biblicalTermsListType: "Major",
                biblicalTermsFileName: "BiblicalTerms.xml"
            ),
            preferTermsLocalization: true
        );
        IList<TextRow> rows = env.Corpus.GetRows().ToList();
        Assert.That(rows.Count, Is.EqualTo(5726));
        Assert.That(string.Join(" ", rows.First().Segment), Is.EqualTo("Abagtha"));
    }

    [Test]
    public void TestGetKeyTermsFromTermsLocalizations_()
    {
        var env = new TestEnvironment(
            new DefaultParatextProjectSettings(
                biblicalTermsListType: "Major",
                biblicalTermsFileName: "BiblicalTerms.xml",
                languageCode: "fr"
            ),
            preferTermsLocalization: true
        );
        IList<TextRow> rows = env.Corpus.GetRows().ToList();
        Assert.That(rows.Count, Is.EqualTo(5716));
        Assert.That(string.Join(" ", rows.First().Segment), Is.EqualTo("Aaron"));
    }

    [Test]
    public void TestGetKeyTermsFromTermsLocalizations_TermRenderingsExists_PreferLocalization()
    {
        var env = new TestEnvironment(
            new DefaultParatextProjectSettings(
                biblicalTermsListType: "Major",
                biblicalTermsFileName: "BiblicalTerms.xml"
            ),
            files: new Dictionary<string, string>()
            {
                {
                    "TermRenderings.xml",
                    @"
<TermRenderingsList>
  <TermRendering Id=""אֲחַשְׁוֵרוֹשׁ"" Guess=""false"">
    <Renderings>Xerxes</Renderings>
    <Glossary />
    <Changes />
    <Notes />
    <Denials />
  </TermRendering>
</TermRenderingsList>"
                }
            },
            preferTermsLocalization: true
        );
        IList<TextRow> rows = env.Corpus.GetRows().ToList();
        Assert.That(rows.Count, Is.EqualTo(5726));
        Assert.That(string.Join(" ", rows.First().Segment), Is.EqualTo("Abagtha"));
    }

    [Test]
    [TestCase("", "")]
    [TestCase("(inside)", "")]
    [TestCase("Outside (inside)", "Outside ")]
    [TestCase("(Inside (inside)) Outside (Inside) (", " Outside  (")]
    [TestCase("[inside] (outside)", " (outside)", '[', ']')]
    public void TestStripParens(string testString, string expectedOutput, char left = '(', char right = ')')
    {
        Assert.That(
            ParatextTermsCorpusBase.StripParens(testString, left: left, right: right),
            Is.EqualTo(expectedOutput)
        );
    }

    [Test]
    [TestCase("", new string[] { })]
    [TestCase("*Abba* /", new string[] { "Abba" })]
    [TestCase("Abba|| ", new string[] { "Abba" })]
    [TestCase("Abba||Abbah?", new string[] { "Abba", "Abbah" })]
    [TestCase("Abba (note)", new string[] { "Abba" })]
    [TestCase("Abba (note)", new string[] { "Abba" })]
    [TestCase("Ahasuerus, Xerxes; Assuerus", new string[] { "Ahasuerus", "Xerxes", "Assuerus" })]
    public void TestGetGlosses(string glossString, IReadOnlyList<string> expectedOutput)
    {
        Assert.That(ParatextTermsCorpusBase.GetGlosses(glossString), Is.EqualTo(expectedOutput));
    }

    private class TestEnvironment(
        ParatextProjectSettings? settings = null,
        Dictionary<string, string>? files = null,
        bool preferTermsLocalization = false
    )
    {
        public MemoryParatextProjectTermsCorpus Corpus { get; } =
            new MemoryParatextProjectTermsCorpus(
                settings ?? new DefaultParatextProjectSettings(),
                new string[] { "PN" },
                files ?? new(),
                preferTermsLocalization
            );
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