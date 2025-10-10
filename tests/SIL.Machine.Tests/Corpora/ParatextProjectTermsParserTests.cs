using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ParatextProjectTermsParserTests
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
        IEnumerable<(string TermId, IReadOnlyList<string> Glosses)> terms = env.GetGlosses();
        Assert.That(terms.Count, Is.EqualTo(1));
        Assert.That(string.Join(" ", terms.First().Glosses), Is.EqualTo("Xerxes"));
    }

    [Test]
    public void TestGetKeyTermsFromTermsLocalizations_NoTermRenderings()
    {
        var env = new TestEnvironment(
            new MemoryParatextProjectFileHandler.DefaultParatextProjectSettings(
                biblicalTermsListType: "Major",
                biblicalTermsFileName: "BiblicalTerms.xml"
            ),
            useTermGlosses: true
        );
        IEnumerable<(string TermId, IReadOnlyList<string> Glosses)> terms = env.GetGlosses();
        Assert.That(terms.Count, Is.EqualTo(5726));
        Assert.That(string.Join(" ", terms.First().Glosses), Is.EqualTo("Abagtha"));
    }

    [Test]
    public void TestGetKeyTermsFromTermsLocalizations_NoTermRenderings_DoNotUseTermGlosses()
    {
        var env = new TestEnvironment(
            new MemoryParatextProjectFileHandler.DefaultParatextProjectSettings(
                biblicalTermsListType: "Major",
                biblicalTermsFileName: "BiblicalTerms.xml"
            ),
            useTermGlosses: false
        );
        IEnumerable<(string TermId, IReadOnlyList<string> Glosses)> terms = env.GetGlosses();
        Assert.That(terms.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestGetKeyTermsFromTermsLocalizations()
    {
        var env = new TestEnvironment(
            new MemoryParatextProjectFileHandler.DefaultParatextProjectSettings(
                biblicalTermsListType: "Major",
                biblicalTermsFileName: "BiblicalTerms.xml",
                languageCode: "fr"
            ),
            useTermGlosses: true
        );
        IEnumerable<(string TermId, IReadOnlyList<string> Glosses)> terms = env.GetGlosses();
        Assert.That(terms.Count, Is.EqualTo(5715));
        Assert.That(string.Join(" ", terms.First().Glosses), Is.EqualTo("Aaron"));
    }

    [Test]
    public void TestGetKeyTermsFromTermsLocalizations_FilterByChapters()
    {
        var env = new TestEnvironment(
            new MemoryParatextProjectFileHandler.DefaultParatextProjectSettings(
                biblicalTermsListType: "Major",
                biblicalTermsFileName: "BiblicalTerms.xml",
                languageCode: "fr"
            ),
            useTermGlosses: true,
            chapters: new Dictionary<string, HashSet<int>>()
            {
                {
                    "HAB",
                    new() { 1 }
                }
            }
        );
        IEnumerable<(string TermId, IReadOnlyList<string> Glosses)> terms = env.GetGlosses();
        Assert.That(terms.Count, Is.EqualTo(3)); //Habakkuk, YHWH, Kashdi/Chaldean are the only PN terms in HAB 1
        Assert.That(string.Join(" ", terms.First().Glosses), Is.EqualTo("Habaquq"));
    }

    [Test]
    public void TestGetKeyTermsFromTermsLocalizations_TermRenderingsExists_PreferLocalization()
    {
        var env = new TestEnvironment(
            new MemoryParatextProjectFileHandler.DefaultParatextProjectSettings(
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
        IReadOnlyList<(string TermId, IReadOnlyList<string> Glosses)> terms = env.GetGlosses().ToList();
        Assert.That(terms.Count, Is.EqualTo(5726));
        Assert.That(string.Join(" ", terms[1].Glosses), Is.EqualTo("Abagtha"));
        Assert.That(string.Join(" ", terms[2].Glosses), Is.EqualTo("Abi"));
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
            ParatextProjectTermsParserBase.StripParens(testString, left: left, right: right),
            Is.EqualTo(expectedOutput)
        );
    }

    [Test]
    [TestCase("", new string[] { })]
    [TestCase("Abba (note)", new string[] { "Abba" })]
    [TestCase("Ahasuerus, Xerxes; Assuerus", new string[] { "Ahasuerus", "Xerxes", "Assuerus" })]
    public void TestGetGlosses(string glossString, IReadOnlyList<string> expectedOutput)
    {
        Assert.That(ParatextProjectTermsParserBase.GetGlosses(glossString), Is.EqualTo(expectedOutput));
    }

    [Test]
    [TestCase("", new string[] { })]
    [TestCase("*Abba*", new string[] { "Abba" })]
    [TestCase("Abba|| ", new string[] { "Abba" })]
    [TestCase("Abba||Abbah", new string[] { "Abba", "Abbah" })]
    [TestCase("Abba (note)", new string[] { "Abba" })]
    public void TestGetRenderings(string renderingString, IReadOnlyList<string> expectedOutput)
    {
        Assert.That(ParatextProjectTermsParserBase.GetRenderings(renderingString), Is.EqualTo(expectedOutput));
    }

    private class TestEnvironment(
        ParatextProjectSettings? settings = null,
        Dictionary<string, string>? files = null,
        bool useTermGlosses = true,
        IDictionary<string, HashSet<int>>? chapters = null
    )
    {
        private readonly bool _useTermGlosses = useTermGlosses;
        private readonly IDictionary<string, HashSet<int>>? _chapters = chapters;

        public ParatextProjectTermsParserBase Parser { get; } = new MemoryParatextProjectTermsParser(files, settings);

        public IEnumerable<(string TermId, IReadOnlyList<string> Glosses)> GetGlosses()
        {
            return Parser.Parse(new string[] { "PN" }, _useTermGlosses, _chapters);
        }
    }
}
