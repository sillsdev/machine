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

        // insert the source into the target as pretranslations to make sure that USFM generation works
        IReadOnlyList<UpdateUsfmRow> pretranslations = rows.Select(r => new UpdateUsfmRow(
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
            string newUsfm = updater.UpdateUsfm(
                bookId,
                pretranslations,
                textBehavior: UpdateUsfmTextBehavior.StripExisting
            );
            Assert.That(newUsfm, Is.Not.Null);
        }
    }

    public record PretranslationDto
    {
        public required string TextId { get; init; }
        public required IReadOnlyList<string> Refs { get; init; }
        public required string Translation { get; init; }
    }

    public static readonly string PretranslationPath = Path.Combine(
        CorporaTestHelpers.TestDataPath,
        "pretranslations.json"
    );
    public static readonly string ParatextProjectPath = Path.Combine(CorporaTestHelpers.TestDataPath, "project");

    [Test]
    [Ignore("This is for manual testing only.  Remove this tag to run the test.")]
    /*
   In order to run this test on specific projects, place the Paratext projects or Paratext project zips in the Corpora/TestData/project/ folder.
   If only testing one project, you can instead place the project in the Corpora/TestData/ folder and rename it to "project"
   */
    public async Task CreateUsfmFile()
    {
        async Task GetUsfmAsync(string projectPath)
        {
            ParatextProjectSettingsParserBase parser;
            ZipArchive? projectArchive = null;
            try
            {
                projectArchive = ZipFile.Open(projectPath, ZipArchiveMode.Read);
                parser = new ZipParatextProjectSettingsParser(projectArchive);
            }
            catch (UnauthorizedAccessException)
            {
                parser = new FileParatextProjectSettingsParser(projectPath);
            }
            ParatextProjectSettings settings = parser.Parse();

            // Read text from pretranslations file
            using Stream pretranslationStream = File.OpenRead(PretranslationPath);
            UpdateUsfmRow[] pretranslations = await JsonSerializer
                .DeserializeAsyncEnumerable<PretranslationDto>(
                    pretranslationStream,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
                .Select(p => new UpdateUsfmRow(
                    (IReadOnlyList<ScriptureRef>)(
                        p?.Refs.Select(r => ScriptureRef.Parse(r, settings.Versification).ToRelaxed()).ToArray() ?? []
                    ),
                    p?.Translation ?? ""
                ))
                .ToArrayAsync();
            List<string> bookIds = [];
            ParatextProjectTextUpdaterBase updater;
            if (projectArchive == null)
            {
                bookIds = (
                    Directory
                        .EnumerateFiles(projectPath, $"{settings.FileNamePrefix}*{settings.FileNameSuffix}")
                        .Select(path => new DirectoryInfo(path).Name)
                        .Select(filename =>
                        {
                            string bookId;
                            if (settings.IsBookFileName(filename, out bookId))
                                return bookId;
                            else
                                return "";
                        })
                        .Where(id => id != "")
                ).ToList();
                updater = new FileParatextProjectTextUpdater(projectPath);
            }
            else
            {
                bookIds = projectArchive
                    .Entries.Where(e =>
                        e.Name.StartsWith(settings.FileNamePrefix) && e.Name.EndsWith(settings.FileNameSuffix)
                    )
                    .Select(e =>
                    {
                        string bookId;
                        if (settings.IsBookFileName(e.Name, out bookId))
                            return bookId;
                        else
                            return "";
                    })
                    .Where(id => id != "")
                    .ToList();
                updater = new ZipParatextProjectTextUpdater(projectArchive);
            }
            foreach (string bookId in bookIds)
            {
                string newUsfm = updater.UpdateUsfm(
                    bookId,
                    pretranslations,
                    textBehavior: UpdateUsfmTextBehavior.StripExisting
                );
                Assert.That(newUsfm, Is.Not.Null);
            }
        }
        if (!File.Exists(Path.Combine(ParatextProjectPath, "Settings.xml")))
        {
            Assert.Multiple(() =>
            {
                foreach (string subdir in Directory.EnumerateFiles(ParatextProjectPath))
                    Assert.DoesNotThrowAsync(async () => await GetUsfmAsync(subdir), $"Failed to parse {subdir}");
            });
        }
        else
        {
            await GetUsfmAsync(ParatextProjectPath);
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
}
