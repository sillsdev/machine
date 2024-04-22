using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UsfmParserTests
{
    [Test]
    [Ignore("This is for manual testing only.  Remove this tag to run the test.")]
    public void ParseParallelCorpus()
    {
        var tCorpus = new ParatextTextCorpus(
            projectDir: CorporaTestHelpers.UsfmTargetProjectPath,
            includeAllText: true,
            includeMarkers: true
        );

        var sCorpus = new ParatextTextCorpus(
            projectDir: CorporaTestHelpers.UsfmSourceProjectPath,
            includeAllText: true,
            includeMarkers: true
        );

        ParallelTextCorpus pCorpus = new ParallelTextCorpus(
            sCorpus,
            tCorpus,
            alignmentCorpus: null,
            rowRefComparer: null
        )
        {
            AllSourceRows = true,
            AllTargetRows = false
        };

        var rows = pCorpus.GetRows().ToList();
        Assert.That(rows.Count, Is.Not.Zero);
    }
}
