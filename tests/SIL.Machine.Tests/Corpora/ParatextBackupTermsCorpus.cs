using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ParatextBackupTermsCorpusTests
{
    [Test]
    public void CreateCorpus()
    {
        string backupDir = CorporaTestHelpers.CreateTestParatextBackup();
        var corpus = new ParatextBackupTermsCorpus(backupDir, new string[] { "PN" }, true);
        IList<TextRow> rows = corpus.GetRows().ToList();
        Assert.That(rows.Count, Is.EqualTo(1));
        Assert.That(rows.First().Text, Is.EqualTo("Xerxes"));
    }
}
