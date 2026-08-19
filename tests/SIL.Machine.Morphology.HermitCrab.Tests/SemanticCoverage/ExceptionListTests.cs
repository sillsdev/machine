using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Pins conformance/README.md's "named asterisk" exception list against the two ledgers that actually
/// prove "no engine consumer": `no-consumer` rows in semantic-coverage-proofs.tsv (a surface a fixture
/// declares, but mutating it changes nothing) and `dead-schema` rows in semantic-coverage-baseline.txt
/// (a surface no fixture ever attempts, because the engine never reads the owning element at all). This
/// test checks both ledgers together against the README: checking only the one that historically fed
/// it would leave dead-schema surfaces of the same "no engine reads this" shape as the two named
/// exceptions unnamed. These tests fail closed the moment either ledger gains a surface the README
/// does not name, or the README's own count of eleven stops matching the ledgers.
/// </summary>
[TestFixture]
public sealed class ExceptionListTests
{
    private const string StartMarker = "<!-- exception-surfaces:start -->";
    private const string EndMarker = "<!-- exception-surfaces:end -->";

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

    private static string ReadExceptionListSection(string root)
    {
        string path = Path.Combine(root, "conformance", "README.md");
        string text = File.ReadAllText(path);
        int start = text.IndexOf(StartMarker, StringComparison.Ordinal);
        int end = text.IndexOf(EndMarker, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"{StartMarker} not found in conformance/README.md");
        Assert.That(
            end,
            Is.GreaterThan(start),
            $"{EndMarker} not found after the start marker in conformance/README.md"
        );
        return text[start..end];
    }

    /// <summary>The words a reader would need to see to recognize this surface was named: the DTD
    /// element for an element surface, or the attribute and value for an enumerated surface.</summary>
    private static IReadOnlyList<string> SurfaceKeywords(string surfaceId)
    {
        if (surfaceId.StartsWith(GrammarFeatureUsage.ElementPrefix, StringComparison.Ordinal))
            return new[] { surfaceId[GrammarFeatureUsage.ElementPrefix.Length..] };

        if (surfaceId.StartsWith(GrammarFeatureUsage.EnumPrefix, StringComparison.Ordinal))
        {
            string[] parts = surfaceId[GrammarFeatureUsage.EnumPrefix.Length..].Split('/');
            Assert.That(parts, Has.Length.EqualTo(3), $"unexpected enum surface shape: {surfaceId}");
            return new[] { parts[1], parts[2] };
        }

        Assert.Fail($"unrecognized surface id shape: {surfaceId}");
        return Array.Empty<string>();
    }

    [Test]
    public void EveryNoConsumerProofIsNamedInTheReadmeExceptionList()
    {
        string root = RepositoryRoot();
        string section = ReadExceptionListSection(root);
        IReadOnlyList<ImpossibilityProof> noConsumerProofs = ImpossibilityProofs
            .Read(root)
            .Where(proof => proof.Kind == ImpossibilityProofs.NoConsumer)
            .ToArray();

        Assert.That(noConsumerProofs, Is.Not.Empty, "the no-consumer ledger should not be empty");

        string[] missing = noConsumerProofs
            .Where(proof =>
                SurfaceKeywords(proof.SurfaceId).Any(keyword => !section.Contains(keyword, StringComparison.Ordinal))
            )
            .Select(proof => proof.SurfaceId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.That(
            missing,
            Is.Empty,
            $"these no-consumer proofs are not named in conformance/README.md's exception list:\n  {string.Join("\n  ", missing)}"
        );
    }

    [Test]
    public void EveryDeadSchemaSurfaceIsNamedInTheReadmeExceptionList()
    {
        string root = RepositoryRoot();
        string section = ReadExceptionListSection(root);
        IReadOnlyList<GrammarCoverageGate.LedgerEntry> deadSchemaEntries = GrammarCoverageGate
            .ReadBaseline(root)
            .Where(entry => entry.Classification == GrammarCoverageGate.DeadSchema)
            .ToArray();

        Assert.That(deadSchemaEntries, Is.Not.Empty, "the dead-schema ledger should not be empty");

        string[] missingElements = deadSchemaEntries
            .Select(entry => DeadSchemaDetector.OwningElement(entry.SurfaceId))
            .Where(name => name is not null)
            .Select(name => name!)
            .Distinct(StringComparer.Ordinal)
            .Where(name => !section.Contains(name, StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(
            missingElements,
            Is.Empty,
            $"these dead-schema elements are not named in conformance/README.md's exception list:\n  {string.Join("\n  ", missingElements)}"
        );
    }

    // Both surface counts are recomputed, not hand-copied, so a future proof or a future dead-schema
    // finding changes this number the moment it lands -- exactly the drift the defect exploited: a
    // mechanically true fact that only one of the two ledgers surfaced to the README's headline claim.
    [Test]
    public void TheReadmeClaimsExactlyAsManySurfacesAsBothLedgersRecord()
    {
        string root = RepositoryRoot();
        int noConsumerCount = ImpossibilityProofs
            .Read(root)
            .Count(proof => proof.Kind == ImpossibilityProofs.NoConsumer);
        int deadSchemaCount = GrammarCoverageGate
            .ReadBaseline(root)
            .Count(entry => entry.Classification == GrammarCoverageGate.DeadSchema);
        int total = noConsumerCount + deadSchemaCount;

        string readmePath = Path.Combine(root, "conformance", "README.md");
        string readme = File.ReadAllText(readmePath);

        Assert.That(
            readme,
            Does.Contain($"{ToWords(total)} surfaces across three feature areas"),
            $"conformance/README.md must claim exactly {total} surfaces ({noConsumerCount} no-consumer + {deadSchemaCount} dead-schema); update its wording if this count changed"
        );
    }

    private static string ToWords(int count) =>
        count switch
        {
            11 => "Eleven",
            _ => count.ToString(),
        };
}
