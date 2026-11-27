using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class WordsYamlGeneratedNoParseTests
{
    private const string Words =
        "words:\n  - word: kat\n    note: n\n    parses:\n      - signature: \"KAT|kat\"\n        rules: []\n";

    private static WordsYaml Load(string frontMatter)
    {
        string path = Path.Combine(Path.GetTempPath(), "words-" + Guid.NewGuid().ToString("N") + ".yaml");
        File.WriteAllText(path, "language: T\n" + frontMatter + Words);
        try
        {
            return WordsYamlLoader.Load(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ExpandsInLengthThenAlphabetOrderAndListedWordsWin()
    {
        WordsYaml words = Load("generated_no_parse:\n  - alphabet: [k, a, t]\n    max_length: 3\n    note: gen\n");

        Assert.That(words.Words[0].Word, Is.EqualTo("kat"));
        Assert.That(words.Words[0].ExpectFail, Is.False);
        string[] generated = words.Words.Skip(1).Select(w => w.Word).ToArray();
        Assert.That(generated, Has.Length.EqualTo(3 + 9 + 27 - 1));
        Assert.That(generated.Take(8), Is.EqualTo(new[] { "k", "a", "t", "kk", "ka", "kt", "ak", "aa" }));
        Assert.That(generated, Does.Not.Contain("kat"));
        Assert.That(words.Words.Skip(1), Has.All.Matches<WordEntry>(w => w.ExpectFail && w.Note == "gen"));
    }

    [Test]
    public void MinLengthAndOverlappingRulesNeverRepeatAWord()
    {
        WordsYaml words = Load(
            "generated_no_parse:\n"
                + "  - alphabet: [a]\n    max_length: 2\n    note: one\n"
                + "  - alphabet: [a, \"+\"]\n    min_length: 2\n    max_length: 2\n    note: two\n"
        );

        Assert.That(
            words.Words.Skip(1).Select(w => w.Word + "/" + w.Note),
            Is.EqualTo(new[] { "a/one", "aa/one", "a+/two", "+a/two", "++/two" })
        );
    }

    [Test]
    public void TheRuleCarriesItsLabel()
    {
        WordsYaml words = Load(
            "generated_no_parse:\n  - alphabet: [a]\n    max_length: 1\n    note: n\n    exercises: [x]\n"
        );

        Assert.That(words.Words[1].Exercises, Is.EqualTo(new[] { "x" }));
    }

    [TestCase("generated_no_parse:\n  - alphabet: [a]\n    note: n\n", "max_length")]
    [TestCase("generated_no_parse:\n  - alphabet: [a]\n    max_length: 2\n", "note")]
    [TestCase(
        "generated_no_parse:\n  - alphabet: [a]\n    max_length: 2\n    note: n\n    parses: []\n",
        "unknown key"
    )]
    [TestCase(
        "generated_no_parse:\n  - alphabet: [a]\n    min_length: 3\n    max_length: 2\n    note: n\n",
        "min_length"
    )]
    [TestCase("generated_no_parse:\n  - alphabet: [a, a]\n    max_length: 2\n    note: n\n", "distinct")]
    [TestCase(
        "generated_no_parse:\n  - alphabet: [a, b, c, d, e, f, g, h, i, j]\n    max_length: 6\n    note: n\n",
        "100000"
    )]
    [TestCase("generated_no_parse:\n  - alphabet: [a, b]\n    max_length: 4000000\n    note: n\n", "100000")]
    [TestCase(
        "generated_no_parse:\n  - alphabet: [a, b]\n    min_length: 4000000\n    max_length: 4000001\n    note: n\n",
        "100000"
    )]
    public void ABadRuleIsRefused(string frontMatter, string expected)
    {
        var ex = Assert.Throws<WordsYamlException>(() => Load(frontMatter));

        Assert.That(ex!.Message, Does.Contain(expected));
    }
}
