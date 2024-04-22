using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UsfmParserTests
{
    [Test]
    public void ParseParallelCorpus()
    {
        // To test the parsing of a set of USFM files, place them in the target and source paths and run this test.
        if (
            !Directory.EnumerateFiles(CorporaTestHelpers.UsfmTargetProjectPath).Any()
            || !Directory.EnumerateFiles(CorporaTestHelpers.UsfmSourceProjectPath).Any()
        )
        {
            Assert.Ignore("No files found in the target project directory.");
        }

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
