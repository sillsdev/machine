using NUnit.Framework;
using SIL.ObjectModel;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ParatextKeyTermsCorpusTests
{
    [Test]
    public void TestGetKeyTerms()
    {
        using var env = new TestEnvironment();
        IList<TextRow> rows = env.Corpus.GetRows().ToList();
        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(string.Join(" ", rows.First().Segment), Is.EqualTo("Abba"));
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
            ParatextBackupTermsCorpus.StripParens(testString, left: left, right: right),
            Is.EqualTo(expectedOutput)
        );
    }

    [Test]
    [TestCase("", new string[] { })]
    [TestCase("*Abba* /", new string[] { "Abba" })]
    [TestCase("Abba|| ", new string[] { "Abba" })]
    [TestCase("Abba||Abbah?", new string[] { "Abba", "Abbah" })]
    [TestCase("Abba (note)", new string[] { "Abba" })]
    public void TestGetGlosses(string glossString, IReadOnlyList<string> expectedOutput)
    {
        Assert.That(ParatextBackupTermsCorpus.GetRenderings(glossString), Is.EqualTo(expectedOutput));
    }

    private class TestEnvironment : DisposableBase
    {
        private readonly string _backupPath;

        public TestEnvironment()
        {
            _backupPath = CorporaTestHelpers.CreateTestParatextBackup();
            Corpus = new ParatextBackupTermsCorpus(_backupPath, new string[] { "PN" });
        }

        public ParatextBackupTermsCorpus Corpus { get; }

        protected override void DisposeManagedResources()
        {
            if (File.Exists(_backupPath))
                File.Delete(_backupPath);
        }
    }
}
