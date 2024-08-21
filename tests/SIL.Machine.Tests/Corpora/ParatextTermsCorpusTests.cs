using System.Text;
using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ParatextTermsCorpusTests
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
            ),
            useTermGlosses: true
        );
        IList<TextRow> rows = env.Corpus.GetRows().ToList();
        Assert.That(rows.Count, Is.EqualTo(5726));
        Assert.That(string.Join(" ", rows.First().Segment), Is.EqualTo("Abagtha"));
    }

    [Test]
    public void TestGetKeyTermsFromTermsLocalizations_NoTermRenderings_DoNotUseTermGlosses()
    {
        var env = new TestEnvironment(
            new DefaultParatextProjectSettings(
                biblicalTermsListType: "Major",
                biblicalTermsFileName: "BiblicalTerms.xml"
            ),
            useTermGlosses: false
        );
        IList<TextRow> rows = env.Corpus.GetRows().ToList();
        Assert.That(rows.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestGetKeyTermsFromTermsLocalizations_NoTermRenderings_PreferLocalization()
    {
        var env = new TestEnvironment(
            new DefaultParatextProjectSettings(
                biblicalTermsListType: "Major",
                biblicalTermsFileName: "BiblicalTerms.xml"
            ),
            useTermGlosses: true
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
            useTermGlosses: true
        );
        IList<TextRow> rows = env.Corpus.GetRows().ToList();
        Assert.That(rows.Count, Is.EqualTo(5715));
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
            useTermGlosses: true
        );
        IList<TextRow> rows = env.Corpus.GetRows().ToList();
        Assert.That(rows.Count, Is.EqualTo(5726));
        Assert.That(string.Join(" ", rows.First().Segment), Is.EqualTo("Xerxes"));
        Assert.That(string.Join(" ", rows[2].Segment), Is.EqualTo("Abi"));
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
            ParatextTermsParserBase.StripParens(testString, left: left, right: right),
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
        Assert.That(ParatextTermsParserBase.GetGlosses(glossString), Is.EqualTo(expectedOutput));
    }

    private class TestEnvironment(
        ParatextProjectSettings? settings = null,
        Dictionary<string, string>? files = null,
        bool useTermGlosses = true
    )
    {
        public ParatextProjectTermsCorpus Corpus { get; } =
            new ParatextProjectTermsCorpus(
                files ?? new(),
                settings ?? new DefaultParatextProjectSettings(),
                new string[] { "PN" },
                useTermGlosses
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
