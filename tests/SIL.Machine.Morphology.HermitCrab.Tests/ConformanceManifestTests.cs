using System.Globalization;
using System.Text.Json;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ConformanceManifestTests
{
    private static string SuiteRoot() => ConformanceSuite.Root();

    private static ConformanceManifest Manifest() => ConformanceManifestGenerator.Generate(SuiteRoot());

    /// <summary>A path as the manifest names it: relative to the suite root, the conformance suite.</summary>
    private static string InSuite(string manifestPath) =>
        Path.Combine(SuiteRoot(), manifestPath.Replace('/', Path.DirectorySeparatorChar));

    private const string LibraryDtdRelativePath = "src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd";

    private static string LibraryDtdPath =>
        Path.Combine(
            ConformanceSuite.RepositoryRoot(),
            LibraryDtdRelativePath.Replace('/', Path.DirectorySeparatorChar)
        );

    [Test]
    public void FormatIdentifiersAreExact()
    {
        ConformanceManifest manifest = Manifest();

        Assert.Multiple(() =>
        {
            Assert.That(manifest.FormatVersion, Is.EqualTo("hc-conformance-manifest/v2"));
            Assert.That(manifest.WordsAuthoringFormatVersion, Is.EqualTo("hc-conformance-words/v1"));
            Assert.That(manifest.GrammarFormatVersion, Is.EqualTo("sil-machine-hermit-crab-input-xml/v1"));
            Assert.That(manifest.SourceHash, Has.Length.EqualTo(64));
            Assert.That(manifest.DtdSha256, Has.Length.EqualTo(64));
            Assert.That(manifest.Fixtures, Is.Not.Empty);
        });
    }

    // The suite is consumed on its own, so every path the manifest names is relative to the suite root
    // and carries no directory prefix.
    [Test]
    public void EveryPathTheManifestNamesIsRelativeToTheSuiteRoot()
    {
        ConformanceManifest manifest = Manifest();

        Assert.Multiple(() =>
        {
            Assert.That(manifest.DtdPath, Is.EqualTo("HermitCrabInput.dtd"));
            Assert.That(File.Exists(InSuite(manifest.DtdPath)), Is.True, manifest.DtdPath);
            foreach (ManifestFixture fixture in manifest.Fixtures)
            {
                Assert.That(fixture.GrammarPath, Is.EqualTo($"{fixture.FixtureId}/grammar.xml"));
                Assert.That(fixture.WordsPath, Is.EqualTo($"{fixture.FixtureId}/words.yaml"));
                Assert.That(File.Exists(InSuite(fixture.GrammarPath)), Is.True, fixture.GrammarPath);
                Assert.That(File.Exists(InSuite(fixture.WordsPath)), Is.True, fixture.WordsPath);
                Assert.That(fixture.GrammarPath, Does.Not.Contain("\\"));
                Assert.That(fixture.GrammarSha256, Has.Length.EqualTo(64));
                Assert.That(fixture.WordsSha256, Has.Length.EqualTo(64));
                Assert.That(fixture.DisplayLanguage, Is.Not.Empty, fixture.FixtureId);
                Assert.That(fixture.CaseCount, Is.GreaterThan(0), fixture.FixtureId);
                Assert.That(
                    fixture.Category,
                    Is.EqualTo(
                        fixture.FixtureId.StartsWith("languages/", StringComparison.Ordinal) ? "language" : "edge-case"
                    )
                );
            }
        });
    }

    // The library reads its DTD as an embedded resource and cannot move; the suite's copy is the
    // authority. They must not drift.
    [Test]
    public void PublishedDtdMatchesTheLibraryResource()
    {
        Assert.That(
            File.ReadAllText(InSuite(ConformanceManifestGenerator.DtdRelativePath)).ReplaceLineEndings("\n"),
            Is.EqualTo(File.ReadAllText(LibraryDtdPath).ReplaceLineEndings("\n")),
            $"the suite's {ConformanceManifestGenerator.DtdRelativePath} is authoritative: change it in the suite, bump "
                + $"conformance.commit and copy it over {LibraryDtdRelativePath}"
        );
    }

    [Test]
    public void CrashFixturesDeclareExactlyOneCase()
    {
        Assert.Multiple(() =>
        {
            foreach (ManifestFixture fixture in Manifest().Fixtures.Where(item => item.ExpectedCrash))
                Assert.That(fixture.CaseCount, Is.EqualTo(1), fixture.FixtureId);
        });
    }

    // A regenerated manifest must be byte-identical, or `--check-manifest` cannot detect drift.
    [Test]
    public void GenerationIsDeterministicAndCanonical()
    {
        string first = ManifestJson.Serialize(Manifest());
        string second = ManifestJson.Serialize(Manifest());

        Assert.That(second, Is.EqualTo(first));
        Assert.That(() => JsonDocument.Parse(first), Throws.Nothing);
    }

    [Test]
    public void TheCheckedInManifestMatchesRegeneration()
    {
        string path = InSuite(ConformanceManifestGenerator.ManifestRelativePath);
        const string Fix = "regenerate it in the suite with: python3 scripts/generate_manifest.py";

        Assert.That(File.Exists(path), Is.True, Fix);
        Assert.That(
            File.ReadAllText(path).ReplaceLineEndings("\n"),
            Is.EqualTo(ManifestJson.Serialize(Manifest()) + "\n"),
            Fix
        );
    }

    [Test]
    public void TheManifestValidatesAgainstItsPublishedSchema()
    {
        Json.Schema.JsonSchema schema = Json.Schema.JsonSchema.FromFile(
            InSuite("schema/conformance-manifest.schema.json")
        );
        using JsonDocument document = JsonDocument.Parse(ManifestJson.Serialize(Manifest()));

        Json.Schema.EvaluationResults results = schema.Evaluate(
            document.RootElement,
            new Json.Schema.EvaluationOptions { OutputFormat = Json.Schema.OutputFormat.List }
        );

        IEnumerable<string> failures = (results.Details ?? new List<Json.Schema.EvaluationResults>())
            .Where(detail => detail.Errors is { Count: > 0 })
            .SelectMany(detail => detail.Errors!.Select(error => $"{detail.InstanceLocation}: {error.Value}"))
            .Take(20);

        Assert.That(results.IsValid, Is.True, string.Join(Environment.NewLine, failures));
    }

    // words.yaml is the canonical authored format, so the published schema has to describe every
    // file the loader already accepts.
    [Test]
    public void EveryWordsFileValidatesAgainstThePublishedSchema()
    {
        string schema = InSuite(WordsSchemaValidation.SchemaRelativePath);

        Assert.Multiple(() =>
        {
            foreach (ManifestFixture fixture in Manifest().Fixtures)
            {
                IReadOnlyList<string> violations = WordsSchemaValidation.Validate(InSuite(fixture.WordsPath), schema);
                Assert.That(violations, Is.Empty, $"{fixture.FixtureId}: {string.Join(" | ", violations.Take(6))}");
            }
        });
    }

    [TestCase("anchors", "language: X\nwords: &a []\n")]
    [TestCase("merge keys", "language: X\nwords:\n  - <<: {word: a}\n    note: n\n")]
    public void PlainYamlOnlyIsEnforced(string _, string yaml)
    {
        string schema = InSuite(WordsSchemaValidation.SchemaRelativePath);
        string path = Path.Combine(Path.GetTempPath(), $"hc-words-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(path, yaml);
        try
        {
            Assert.That(() => WordsSchemaValidation.Validate(path, schema), Throws.TypeOf<InvalidDataException>());
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>A minimal suite carrying one real fixture, for mutation tests.</summary>
    internal static string CreateMiniSuite(string fixtureId)
    {
        string source = SuiteRoot();
        string root = Path.Combine(Path.GetTempPath(), $"hc-manifest-suite-{Guid.NewGuid():N}");

        void Copy(string relative)
        {
            string target = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(source, relative.Replace('/', Path.DirectorySeparatorChar)), target);
        }

        Copy(ConformanceManifestGenerator.DtdRelativePath);
        Copy("constructs.txt");
        Copy("parity-check.py");
        Copy("schema/words.schema.json");
        Copy("schema/conformance-manifest.schema.json");
        Copy($"{fixtureId}/grammar.xml");
        Copy($"{fixtureId}/words.yaml");
        return root;
    }

    // Every input the manifest rests on must move its sourceHash, or a stale manifest cannot be
    // told from a current one.
    [TestCase("{0}/grammar.xml")]
    [TestCase("{0}/words.yaml")]
    [TestCase("constructs.txt")]
    [TestCase("parity-check.py")]
    [TestCase("schema/words.schema.json")]
    public void EverySourceFileMovesTheSourceHash(string relativeTemplate)
    {
        const string FixtureId = "edge-cases/strrep-identity";
        string root = CreateMiniSuite(FixtureId);
        try
        {
            string before = ConformanceManifestGenerator.Generate(root).SourceHash;
            string target = Path.Combine(
                root,
                string.Format(CultureInfo.InvariantCulture, relativeTemplate, FixtureId)
                    .Replace('/', Path.DirectorySeparatorChar)
            );
            File.AppendAllText(target, "\n# drift\n");

            Assert.That(
                ConformanceManifestGenerator.Generate(root).SourceHash,
                Is.Not.EqualTo(before),
                relativeTemplate
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Fixture identity comes from the two declared roots only; a stray directory elsewhere under
    // the suite root must not be able to present itself as a fixture.
    [Test]
    public void DiscoveryAcceptsOnlyTheTwoDeclaredFixtureRoots()
    {
        const string FixtureId = "edge-cases/strrep-identity";
        string root = CreateMiniSuite(FixtureId);
        try
        {
            string legacy = Path.Combine(root, "allomorphy", "strrep-identity");
            Directory.CreateDirectory(legacy);
            foreach (string name in new[] { "grammar.xml", "words.yaml" })
            {
                File.Copy(Path.Combine(root, "edge-cases", "strrep-identity", name), Path.Combine(legacy, name));
            }

            Assert.That(
                ConformanceManifestGenerator.Generate(root).Fixtures.Select(fixture => fixture.FixtureId),
                Is.EqualTo(new[] { FixtureId })
            );
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void EveryGrammarValidatesAgainstThePublishedDtd()
    {
        string dtd = InSuite(ConformanceManifestGenerator.DtdRelativePath);

        Assert.Multiple(() =>
        {
            foreach (ManifestFixture fixture in Manifest().Fixtures)
            {
                Assert.That(
                    GrammarValidation.Validate(InSuite(fixture.GrammarPath), dtd),
                    Is.Empty,
                    $"{fixture.FixtureId}: grammar must validate against the published DTD"
                );
            }
        });
    }

    [Test]
    public void TheResolverAdmitsOnlyThePinnedDtdSystemIdentifier()
    {
        string dtd = InSuite(ConformanceManifestGenerator.DtdRelativePath);
        string directory = Path.Combine(Path.GetTempPath(), $"hc-manifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string hostile = Path.Combine(directory, "grammar.xml");
            File.WriteAllText(
                hostile,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                    + "<!DOCTYPE HermitCrabInput SYSTEM \"../../../etc/passwd\">\n"
                    + "<HermitCrabInput/>\n"
            );

            Assert.That(() => GrammarValidation.Validate(hostile, dtd), Throws.TypeOf<System.Xml.XmlException>());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
