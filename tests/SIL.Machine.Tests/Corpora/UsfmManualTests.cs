using System.IO.Compression;
using System.Text.Json;
using NUnit.Framework;
using SIL.Machine.PunctuationAnalysis;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UsfmManualTests
{
    [Test]
    [Ignore("This is for manual testing only.  Remove this tag to run the test.")]
    public void ParseParallelCorpusAsync()
    {
        ParatextTextCorpus tCorpus =
            new(projectDir: CorporaTestHelpers.UsfmTargetProjectPath, includeAllText: true, includeMarkers: true);

        ParatextTextCorpus sCorpus =
            new(projectDir: CorporaTestHelpers.UsfmSourceProjectPath, includeAllText: true, includeMarkers: true);

        ParallelTextCorpus pCorpus =
            new(sCorpus, tCorpus, alignmentCorpus: null, rowRefComparer: null)
            {
                AllSourceRows = true,
                AllTargetRows = false
            };

        List<ParallelTextRow> rows = pCorpus.GetRows().ToList();
        Assert.That(rows, Has.Count.GreaterThan(0));

        // insert the source into the target as update rows to make sure that USFM generation works
        IReadOnlyList<UpdateUsfmRow> updateRows = rows.Select(r => new UpdateUsfmRow(
                (IReadOnlyList<ScriptureRef>)r.SourceRefs.Select(s => (ScriptureRef)s).ToList(),
                r.SourceText
            ))
            .ToList();

        ParatextProjectSettings targetSettings = new FileParatextProjectSettingsParser(
            CorporaTestHelpers.UsfmTargetProjectPath
        ).Parse();
        var updater = new FileParatextProjectTextUpdater(CorporaTestHelpers.UsfmTargetProjectPath);
        foreach (
            string sfmFileName in Directory
                .EnumerateFiles(
                    CorporaTestHelpers.UsfmTargetProjectPath,
                    $"{targetSettings.FileNamePrefix}*{targetSettings.FileNameSuffix}"
                )
                .Select(path => new DirectoryInfo(path).Name)
        )
        {
            string bookId;
            if (!targetSettings.IsBookFileName(sfmFileName, out bookId))
                continue;
            string newUsfm = updater.UpdateUsfm(bookId, updateRows, textBehavior: UpdateUsfmTextBehavior.StripExisting);
            Assert.That(newUsfm, Is.Not.Null);
        }
    }

    [Test]
    [Ignore("This is for manual testing only.  Remove this tag to run the test.")]
    public void AnalyzeCorporaQuoteConventions()
    {
        var sourceHandler = new QuoteConventionDetector();
        using ZipArchive zipArchive = ZipFile.OpenRead(CorporaTestHelpers.UsfmSourceProjectZipPath);
        var quoteConventionDetector = new ZipParatextProjectQuoteConventionDetector(zipArchive);
        quoteConventionDetector.GetQuoteConventionAnalysis(sourceHandler);

        var targetHandler = new QuoteConventionDetector();
        using ZipArchive zipArchive2 = ZipFile.OpenRead(CorporaTestHelpers.UsfmTargetProjectZipPath);
        var quoteConventionDetector2 = new ZipParatextProjectQuoteConventionDetector(zipArchive2);
        quoteConventionDetector2.GetQuoteConventionAnalysis(targetHandler);

        QuoteConventionAnalysis sourceAnalysis = sourceHandler.DetectQuoteConvention();
        QuoteConventionAnalysis targetAnalysis = targetHandler.DetectQuoteConvention();

        Assert.Multiple(() =>
        {
            Assert.NotNull(sourceAnalysis);
            Assert.NotNull(targetAnalysis);
        });
    }

    [Test]
    [Ignore("This is for manual testing only.  Remove this tag to run the test.")]
    public void ValidateUsfmVersification()
    {
        using ZipArchive zipArchive = ZipFile.OpenRead(CorporaTestHelpers.UsfmSourceProjectZipPath);
        var quoteConventionDetector = new ZipParatextProjectVersificationMismatchDetector(zipArchive);
        IReadOnlyList<UsfmVersificationMismatch> mismatches = quoteConventionDetector.GetUsfmVersificationMismatches();

        Assert.That(
            mismatches,
            Has.Count.EqualTo(0),
            JsonSerializer.Serialize(mismatches, new JsonSerializerOptions { WriteIndented = true })
        );
    }
}
