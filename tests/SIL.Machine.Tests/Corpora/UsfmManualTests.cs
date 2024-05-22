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
        FileParatextProjectSettingsParser parser = new(ParatextProjectPath);
        ParatextProjectSettings settings = parser.Parse();

        // Read text from pretranslations file
        using Stream pretranslationStream = File.OpenRead(PretranslationPath);
        (IReadOnlyList<ScriptureRef>, string)[] pretranslations = await JsonSerializer
            .DeserializeAsyncEnumerable<PretranslationDto>(
                pretranslationStream,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            )
            .Select(p =>
                (
                    (IReadOnlyList<ScriptureRef>)(
                        p?.Refs.Select(r => ScriptureRef.Parse(r, settings.Versification).ToRelaxed()).ToArray() ?? []
                    ),
                    p?.Translation ?? ""
                )
            )
            .ToArrayAsync();

        foreach (
            string sfmFileName in Directory.EnumerateFiles(
                ParatextProjectPath,
                $"{settings.FileNamePrefix}*{settings.FileNameSuffix}"
            )
        )
        {
            var updater = new UsfmTextUpdater(pretranslations, stripAllText: true, preferExistingText: true);
            string usfm = await File.ReadAllTextAsync(sfmFileName);
            UsfmParser.Parse(usfm, updater, settings.Stylesheet, settings.Versification);
            string newUsfm = updater.GetUsfm(settings.Stylesheet);
            Assert.That(newUsfm, Is.Not.Null);
        }
    }
}
