using System.Text.Json;
using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UsfmManualTests
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
    public async Task CreateUsfmFile()
    {
        var parser = new FileParatextProjectSettingsParser(ParatextProjectPath);
        ParatextProjectSettings settings = parser.Parse();

        // Read text from pretranslations file
        Stream pretranslationStream = File.OpenRead(PretranslationPath);
        IAsyncEnumerable<PretranslationDto?>? pretranslations =
            JsonSerializer.DeserializeAsyncEnumerable<PretranslationDto>(
                pretranslationStream,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );

        var pretranslationsList = (await pretranslations.ToListAsync())
            .Where(p => p is not null)
            .Select(p =>
                (
                    (IReadOnlyList<ScriptureRef>)
                        p!.Refs.Select(r => ScriptureRef.Parse(r, settings.Versification)).ToList(),
                    p.Translation
                )
            )
            .OrderBy(p => p.Item1[0])
            .ToList();

        foreach (
            string sfmFileName in Directory.EnumerateFiles(
                ParatextProjectPath,
                $"{settings.FileNamePrefix}*{settings.FileNameSuffix}"
            )
        )
        {
            var updater = new UsfmTextUpdater(
                pretranslationsList,
                stripAllText: true,
                strictComparison: false,
                preferExistingText: true
            );
            var usfm = await File.ReadAllTextAsync(sfmFileName);
            UsfmParser.Parse(usfm, updater, settings.Stylesheet, settings.Versification);
            var newUsfm = updater.GetUsfm(settings.Stylesheet);
            Assert.That(newUsfm, Is.Not.Null);
        }
    }
}
