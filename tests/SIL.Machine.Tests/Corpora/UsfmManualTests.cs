using System.IO.Compression;
using System.Text.Json;
using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class UsfmManualTests
{
    [Test]
    [Ignore("This is for manual testing only.  Remove this tag to run the test.")]
    public async Task ParseParallelCorpusAsync()
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
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> pretranslations = rows.Select(r =>
                ((IReadOnlyList<ScriptureRef>)r.SourceRefs.Select(s => (ScriptureRef)s).ToList(), r.SourceText)
            )
            .ToList();

        ParatextProjectSettings targetSettings = new FileParatextProjectSettingsParser(
            CorporaTestHelpers.UsfmTargetProjectPath
        ).Parse();

        foreach (
            string sfmFileName in Directory.EnumerateFiles(
                CorporaTestHelpers.UsfmTargetProjectPath,
                $"{targetSettings.FileNamePrefix}*{targetSettings.FileNameSuffix}"
            )
        )
        {
            var updater = new UsfmTextUpdater(pretranslations, stripAllText: true, preferExistingText: false);
            string usfm = await File.ReadAllTextAsync(sfmFileName);
            UsfmParser.Parse(usfm, updater, targetSettings.Stylesheet, targetSettings.Versification);
            string newUsfm = updater.GetUsfm(targetSettings.Stylesheet);
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
    // [Ignore("This is for manual testing only.  Remove this tag to run the test.")]
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
            (IReadOnlyList<ScriptureRef>, string)[] pretranslations = await JsonSerializer
                .DeserializeAsyncEnumerable<PretranslationDto>(
                    pretranslationStream,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
                .Select(p =>
                    (
                        (IReadOnlyList<ScriptureRef>)(
                            p?.Refs.Select(r => ScriptureRef.Parse(r, settings.Versification).ToRelaxed()).ToArray()
                            ?? []
                        ),
                        p?.Translation ?? ""
                    )
                )
                .ToArrayAsync();
            List<string> sfmTexts = [];
            if (projectArchive == null)
            {
                sfmTexts = (
                    await Task.WhenAll(
                        Directory
                            .EnumerateFiles(projectPath, $"{settings.FileNamePrefix}*{settings.FileNameSuffix}")
                            .Select(async sfmFileName => await File.ReadAllTextAsync(sfmFileName))
                    )
                ).ToList();
            }
            else
            {
                sfmTexts = projectArchive
                    .Entries.Where(e =>
                        e.Name.StartsWith(settings.FileNamePrefix) && e.Name.EndsWith(settings.FileNameSuffix)
                    )
                    .Select(e =>
                    {
                        string contents;
                        using (var sr = new StreamReader(e.Open()))
                        {
                            contents = sr.ReadToEnd();
                        }
                        return contents;
                    })
                    .ToList();
            }
            foreach (string usfm in sfmTexts)
            {
                var updater = new UsfmTextUpdater(pretranslations, stripAllText: true, preferExistingText: true);
                UsfmParser.Parse(usfm, updater, settings.Stylesheet, settings.Versification);
                string newUsfm = updater.GetUsfm(settings.Stylesheet);
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
}
