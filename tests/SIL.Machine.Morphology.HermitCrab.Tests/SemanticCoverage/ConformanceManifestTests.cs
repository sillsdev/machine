using System.Globalization;
using System.Text.Json;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class ConformanceManifestTests
{
    private static string RepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "conformance", "constructs.txt")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate the repository root.");
        return string.Empty;
    }

    private static ConformanceManifest Manifest() => ConformanceManifestGenerator.Generate(RepositoryRoot());

    private static string Absolute(string relative) =>
        Path.Combine(RepositoryRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

    [Test]
    public void FormatIdentifiersAreExact()
    {
        ConformanceManifest manifest = Manifest();

        Assert.Multiple(() =>
        {
            Assert.That(manifest.FormatVersion, Is.EqualTo("hc-conformance-manifest/v1"));
            Assert.That(manifest.WordsAuthoringFormatVersion, Is.EqualTo("hc-conformance-words/v1"));
            Assert.That(manifest.GrammarFormatVersion, Is.EqualTo("sil-machine-hermit-crab-input-xml/v1"));
            Assert.That(manifest.SourceHash, Has.Length.EqualTo(64));
            Assert.That(manifest.DtdSha256, Has.Length.EqualTo(64));
            Assert.That(manifest.Fixtures, Is.Not.Empty);
        });
    }

    // A consumer receives conformance/ alone, so every path the manifest names has to resolve
    // inside it.
    [Test]
    public void EveryPathTheManifestNamesLivesUnderConformance()
    {
        ConformanceManifest manifest = Manifest();

        Assert.Multiple(() =>
        {
            Assert.That(manifest.DtdPath, Does.StartWith("conformance/"));
            Assert.That(File.Exists(Absolute(manifest.DtdPath)), Is.True, manifest.DtdPath);
            foreach (ManifestFixture fixture in manifest.Fixtures)
            {
                Assert.That(fixture.GrammarPath, Is.EqualTo($"conformance/{fixture.FixtureId}/grammar.xml"));
                Assert.That(fixture.WordsPath, Is.EqualTo($"conformance/{fixture.FixtureId}/words.yaml"));
                Assert.That(File.Exists(Absolute(fixture.GrammarPath)), Is.True, fixture.GrammarPath);
                Assert.That(File.Exists(Absolute(fixture.WordsPath)), Is.True, fixture.WordsPath);
                Assert.That(fixture.GrammarPath, Does.Not.Contain("\\"));
                Assert.That(fixture.GrammarSha256, Has.Length.EqualTo(64));
                Assert.That(fixture.WordsSha256, Has.Length.EqualTo(64));
                Assert.That(fixture.DisplayLanguage, Is.Not.Empty, fixture.FixtureId);
                Assert.That(fixture.CaseCount, Is.GreaterThan(0), fixture.FixtureId);
                Assert.That(
                    fixture.Category,
                    Is.EqualTo(fixture.FixtureId.StartsWith("languages/", StringComparison.Ordinal)
                        ? "language"
                        : "edge-case"));
            }
        });
    }

    // The library reads its DTD as an embedded resource and cannot move; the published copy is what
    // a consumer receives. They must not drift.
    [Test]
    public void PublishedDtdMatchesTheLibraryResource()
    {
        Assert.That(
            File.ReadAllText(Absolute(GrammarCoverageGate.DtdRelativePath)).ReplaceLineEndings("\n"),
            Is.EqualTo(File.ReadAllText(Absolute(GrammarCoverageGate.LibraryDtdRelativePath)).ReplaceLineEndings("\n")),
            $"copy {GrammarCoverageGate.LibraryDtdRelativePath} over {GrammarCoverageGate.DtdRelativePath}");
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
        string path = Absolute("conformance/generated/hc-conformance-manifest.v1.json");

        Assert.That(File.Exists(path), Is.True, "regenerate with: hc-conformance --generate-manifest");
        Assert.That(
            File.ReadAllText(path).ReplaceLineEndings("\n"),
            Is.EqualTo(ManifestJson.Serialize(Manifest()) + "\n"),
            "regenerate with: hc-conformance --generate-manifest");
    }

    [Test]
    public void TheManifestValidatesAgainstItsPublishedSchema()
    {
        Json.Schema.JsonSchema schema = Json.Schema.JsonSchema.FromFile(
            Absolute("conformance/schema/conformance-manifest.schema.json"));
        using JsonDocument document = JsonDocument.Parse(ManifestJson.Serialize(Manifest()));

        Json.Schema.EvaluationResults results = schema.Evaluate(
            document.RootElement,
            new Json.Schema.EvaluationOptions { OutputFormat = Json.Schema.OutputFormat.List });

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
        string schema = Absolute(WordsSchemaValidation.SchemaRelativePath);

        Assert.Multiple(() =>
        {
            foreach (ManifestFixture fixture in Manifest().Fixtures)
            {
                IReadOnlyList<string> violations = WordsSchemaValidation.Validate(Absolute(fixture.WordsPath), schema);
                Assert.That(violations, Is.Empty, $"{fixture.FixtureId}: {string.Join(" | ", violations.Take(6))}");
            }
        });
    }

    [TestCase("anchors", "language: X\nwords: &a []\n")]
    [TestCase("merge keys", "language: X\nwords:\n  - <<: {word: a}\n    note: n\n")]
    public void PlainYamlOnlyIsEnforced(string _, string yaml)
    {
        string schema = Absolute(WordsSchemaValidation.SchemaRelativePath);
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

    /// <summary>A minimal repository carrying one real fixture, for mutation tests.</summary>
    private static string CreateMiniRepository(string fixtureId)
    {
        string source = RepositoryRoot();
        string root = Path.Combine(Path.GetTempPath(), $"hc-manifest-repo-{Guid.NewGuid():N}");

        void Copy(string relative)
        {
            string target = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(source, relative.Replace('/', Path.DirectorySeparatorChar)), target);
        }

        Copy(GrammarCoverageGate.DtdRelativePath);
        Copy("conformance/constructs.txt");
        Copy("conformance/parity-check.py");
        Copy("conformance/schema/words.schema.json");
        Copy("conformance/schema/conformance-manifest.schema.json");
        Copy($"conformance/{fixtureId}/grammar.xml");
        Copy($"conformance/{fixtureId}/words.yaml");
        return root;
    }

    // Every input the manifest rests on must move its sourceHash, or a stale manifest cannot be
    // told from a current one.
    [TestCase("conformance/{0}/grammar.xml")]
    [TestCase("conformance/{0}/words.yaml")]
    [TestCase("conformance/constructs.txt")]
    [TestCase("conformance/parity-check.py")]
    [TestCase("conformance/schema/words.schema.json")]
    public void EverySourceFileMovesTheSourceHash(string relativeTemplate)
    {
        const string FixtureId = "edge-cases/strrep-identity";
        string root = CreateMiniRepository(FixtureId);
        try
        {
            string before = ConformanceManifestGenerator.Generate(root).SourceHash;
            string target = Path.Combine(
                root,
                string.Format(CultureInfo.InvariantCulture, relativeTemplate, FixtureId)
                    .Replace('/', Path.DirectorySeparatorChar));
            File.AppendAllText(target, "\n# drift\n");

            Assert.That(ConformanceManifestGenerator.Generate(root).SourceHash, Is.Not.EqualTo(before), relativeTemplate);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Fixture identity comes from the two declared roots only; a stray directory elsewhere under
    // conformance/ must not be able to present itself as a fixture.
    [Test]
    public void DiscoveryAcceptsOnlyTheTwoDeclaredFixtureRoots()
    {
        const string FixtureId = "edge-cases/strrep-identity";
        string root = CreateMiniRepository(FixtureId);
        try
        {
            string legacy = Path.Combine(root, "conformance", "allomorphy", "strrep-identity");
            Directory.CreateDirectory(legacy);
            foreach (string name in new[] { "grammar.xml", "words.yaml" })
            {
                File.Copy(
                    Path.Combine(root, "conformance", "edge-cases", "strrep-identity", name),
                    Path.Combine(legacy, name));
            }

            Assert.That(
                ConformanceManifestGenerator.Generate(root).Fixtures.Select(fixture => fixture.FixtureId),
                Is.EqualTo(new[] { FixtureId }));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void EveryGrammarValidatesAgainstThePublishedDtd()
    {
        string dtd = Absolute(GrammarCoverageGate.DtdRelativePath);

        Assert.Multiple(() =>
        {
            foreach (ManifestFixture fixture in Manifest().Fixtures)
            {
                Assert.That(
                    GrammarValidation.Validate(Absolute(fixture.GrammarPath), dtd),
                    Is.Empty,
                    $"{fixture.FixtureId}: grammar must validate against the published DTD");
            }
        });
    }

    [Test]
    public void TheResolverAdmitsOnlyThePinnedDtdSystemIdentifier()
    {
        string dtd = Absolute(GrammarCoverageGate.DtdRelativePath);
        string directory = Path.Combine(Path.GetTempPath(), $"hc-manifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string hostile = Path.Combine(directory, "grammar.xml");
            File.WriteAllText(
                hostile,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                    + "<!DOCTYPE HermitCrabInput SYSTEM \"../../../etc/passwd\">\n"
                    + "<HermitCrabInput/>\n");

            Assert.That(() => GrammarValidation.Validate(hostile, dtd), Throws.TypeOf<System.Xml.XmlException>());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
