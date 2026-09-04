using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public sealed class GrammarCoverageGateTests
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

    [Test]
    public void UncoveredSurfacesMatchTheBaselineExactly()
    {
        string root = RepositoryRoot();
        GrammarCoverageResult result = GrammarCoverageGate.Compute(root, GrammarCoverageGate.ReadInventory(root));
        string[] baseline = GrammarCoverageGate.ReadBaseline(root).Select(entry => entry.SurfaceId).ToArray();

        string[] newGaps = result
            .Uncovered.Except(baseline, StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] stale = baseline
            .Except(result.Uncovered, StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                newGaps,
                Is.Empty,
                $"new grammar-coverage gaps; add a fixture that exercises them:\n  {string.Join("\n  ", newGaps)}"
            );
            Assert.That(
                stale,
                Is.Empty,
                $"these are now covered; delete them from {GrammarCoverageGate.BaselineRelativePath}:\n  {string.Join("\n  ", stale)}"
            );
        });
    }

    [Test]
    public void EveryFixtureContributesAtLeastOneObservableSurface()
    {
        string root = RepositoryRoot();
        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(root);
        var empty = new List<string>();
        foreach ((string fixtureId, string grammarPath) in GrammarCoverageGate.DiscoverGrammars(root))
        {
            if (GrammarFeatureUsage.Read(XDocument.Load(grammarPath), inventory).Count == 0)
                empty.Add(fixtureId);
        }

        Assert.That(
            empty,
            Is.Empty,
            "a fixture whose grammar exercises no declared surface cannot contribute coverage"
        );
    }

    // Compute only creates a fixture list while appending to it, so asserting the list is non-empty
    // is a tautology. What is worth pinning is that the named fixture really contains the surface.
    [Test]
    public void EveryCoveredSurfaceIsReproducibleFromTheFixtureItNames()
    {
        string root = RepositoryRoot();
        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(root);
        GrammarCoverageResult result = GrammarCoverageGate.Compute(root, inventory);
        var grammarsById = GrammarCoverageGate
            .DiscoverGrammars(root)
            .ToDictionary(item => item.FixtureId, item => item.GrammarPath, StringComparer.Ordinal);

        Assert.That(result.Covered, Is.Not.Empty);
        var unreproducible = new List<string>();
        foreach (string surfaceId in result.Covered)
        {
            foreach (string fixtureId in result.FixturesBySurface[surfaceId])
            {
                var reread = GrammarFeatureUsage.Read(XDocument.Load(grammarsById[fixtureId]), inventory);
                if (!reread.Contains(surfaceId))
                    unreproducible.Add($"{surfaceId} attributed to {fixtureId}, which does not contain it");
            }
        }

        Assert.That(unreproducible, Is.Empty, string.Join("\n  ", unreproducible));
    }

    [Test]
    public void ObservableSurfacesAreOnlyElementsAndEnumeratedValues()
    {
        string root = RepositoryRoot();
        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(root);
        var byId = inventory.Surfaces.ToDictionary(
            surface => surface.Id,
            surface => surface.Kind,
            StringComparer.Ordinal
        );

        Assert.That(
            GrammarFeatureUsage
                .Observable(inventory)
                .Select(id => byId[id])
                .Distinct(StringComparer.Ordinal)
                .OrderBy(k => k, StringComparer.Ordinal),
            Is.EqualTo(new[] { "element", "enum" })
        );
    }

    [Test]
    public void NoCoveredSurfaceRestsOnPresenceAloneOutsideTheWaiverFile()
    {
        string root = RepositoryRoot();
        IReadOnlyList<SurfaceEvidence> evidence = GrammarCoverageGate.GradeEvidence(
            root,
            GrammarCoverageGate.ReadInventory(root)
        );

        string[] presenceOnly = evidence
            .Where(item => item.Strength == EvidenceStrength.Presence)
            .Select(item => item.SurfaceId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        IReadOnlyList<string> waived = GrammarCoverageGate.ReadPresenceWaivers(root);

        Assert.Multiple(() =>
        {
            Assert.That(
                presenceOnly.Except(waived, StringComparer.Ordinal),
                Is.Empty,
                "a surface counted as covered must have trace, negative-control, or structural evidence"
            );
            Assert.That(
                waived.Except(presenceOnly, StringComparer.Ordinal),
                Is.Empty,
                $"these now have real evidence; delete them from {GrammarCoverageGate.PresenceWaiverRelativePath}"
            );
        });
    }

    /// <summary>Ratchet floors. Raise them when coverage improves; never lower them silently.</summary>
    private const int ObservableSurfaces = 264;
    private const int GeneratedSurfaces = 1059;
    private const int TraceFloor = 71;

    [Test]
    public void TheMeasuredDenominatorIsPinned()
    {
        string root = RepositoryRoot();
        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(root);

        // Without this, editing the DTD (an enumerated attribute to NMTOKEN, say) shrinks the
        // denominator, turns the vanished lines stale, and the ratchet REQUIRES deleting them.
        Assert.Multiple(() =>
        {
            Assert.That(inventory.Surfaces, Has.Count.EqualTo(GeneratedSurfaces), "generated surface count changed");
            Assert.That(
                GrammarFeatureUsage.Observable(inventory),
                Has.Count.EqualTo(ObservableSurfaces),
                "observable surface count changed"
            );
        });
    }

    [Test]
    public void TraceEvidenceIsAchievedAndDoesNotRegress()
    {
        string root = RepositoryRoot();
        IReadOnlyList<SurfaceEvidence> evidence = GrammarCoverageGate.GradeEvidence(
            root,
            GrammarCoverageGate.ReadInventory(root)
        );

        Assert.That(evidence, Is.Not.Empty, "an empty evidence set would make every evidence gate vacuous");
        Assert.That(
            evidence.Count(item => item.Strength == EvidenceStrength.Trace),
            Is.GreaterThanOrEqualTo(TraceFloor),
            "trace-verified coverage regressed"
        );
        foreach (SurfaceEvidence item in evidence.Where(item => item.Strength == EvidenceStrength.Trace))
        {
            // The Presence detail also ends in "fired", so match the prefix, not the word.
            Assert.That(item.Detail, Does.StartWith("rule '"), $"{item.SurfaceId} must name the firing rule");
            Assert.That(item.Detail, Does.EndWith("fired in a verified parse"));
            Assert.That(item.FixtureId, Is.Not.Empty);
        }
    }

    // A value equal to its attribute's DTD default is supplied by the validating parser for every
    // grammar, so no fixture can discriminate writing it from omitting it. Counting those as work
    // items inflates the worklist with certified no-ops.
    [Test]
    public void DtdDefaultValuesAreDetectedFromTheInventoryNotTheLedger()
    {
        string root = RepositoryRoot();
        var declared = GrammarCoverageGate
            .ReadInventory(root)
            .Surfaces.Select(s => s.Id)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(GrammarCoverageGate.IsDtdDefault("dtd:enum/AffixTemplate/isActive/yes", declared), Is.True);
            Assert.That(GrammarCoverageGate.IsDtdDefault("dtd:enum/AffixTemplate/isActive/no", declared), Is.False);
            Assert.That(GrammarCoverageGate.IsDtdDefault("dtd:enum/Stratum/cyclicity/noncyclic", declared), Is.True);
            Assert.That(GrammarCoverageGate.IsDtdDefault("dtd:enum/Stratum/cyclicity/cyclic", declared), Is.False);
            Assert.That(
                GrammarCoverageGate.IsDtdDefault("dtd:element/AffixTemplate", declared),
                Is.False,
                "only enumerated values have defaults"
            );
        });
    }

    [Test]
    public void EveryDtdDefaultLedgerLineIsGenuinelyTheDefault()
    {
        string root = RepositoryRoot();
        var declared = GrammarCoverageGate
            .ReadInventory(root)
            .Surfaces.Select(s => s.Id)
            .ToHashSet(StringComparer.Ordinal);

        string[] wrong = GrammarCoverageGate
            .ReadBaseline(root)
            .Where(entry => entry.Classification == GrammarCoverageGate.DtdDefault)
            .Where(entry => !GrammarCoverageGate.IsDtdDefault(entry.SurfaceId, declared))
            .Select(entry => entry.SurfaceId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.That(wrong, Is.Empty, $"these are not their attribute's default:\n  {string.Join("\n  ", wrong)}");
    }

    [Test]
    public void AQuotientNeedsACoveredSiblingToStand()
    {
        var ledger = new[]
        {
            new GrammarCoverageGate.LedgerEntry("dtd:enum/E/a/x", GrammarCoverageGate.AlphabetQuotient),
            new GrammarCoverageGate.LedgerEntry("dtd:enum/F/b/x", GrammarCoverageGate.AlphabetQuotient),
            new GrammarCoverageGate.LedgerEntry("dtd:enum/G/c/x", GrammarCoverageGate.Todo),
        };

        Assert.That(
            GrammarCoverageGate.UnbackedQuotients(ledger, new[] { "dtd:enum/E/a/y" }),
            Is.EqualTo(new[] { "dtd:enum/F/b/x" }),
            "a quotient whose attribute has no covered sibling claims a mechanism nothing exercises"
        );
        Assert.That(
            GrammarCoverageGate.UnbackedQuotients(ledger, Array.Empty<string>()),
            Is.EqualTo(new[] { "dtd:enum/E/a/x", "dtd:enum/F/b/x" })
        );
    }

    [Test]
    public void UnclassifiedCatalogFeaturesCanOnlyShrink()
    {
        const int UnclassifiedCeiling = 108;
        SemanticCatalog catalog = CatalogBootstrap.Load(RepositoryRoot());

        Assert.That(
            catalog.Features.Count(feature => feature.Disposition == FeatureDisposition.Unclassified),
            Is.LessThanOrEqualTo(UnclassifiedCeiling),
            "the bootstrap's unclassified backlog must not grow; classify the new surfaces"
        );
    }

    // The audit is only worth anything if it runs against the real inventory. Before the catalog was
    // checked in it ran only over literals inside its own tests, so nothing failed closed.
    [Test]
    public void TheCheckedInCatalogMapsEveryRealSurfaceExactlyOnce()
    {
        string root = RepositoryRoot();
        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(root);
        SemanticCatalog catalog = CatalogBootstrap.Load(root);

        AuditResult audit = SemanticCoverageAudit.Run(inventory, catalog);

        Assert.That(audit.IsComplete, Is.False, "the checked-in catalog is intentionally still a proposal backlog");
        Assert.That(
            audit.Diagnostics.Select(item => item.Code).Distinct(StringComparer.Ordinal),
            Is.EquivalentTo(new[] { SemanticCoverageAudit.UnclassifiedMapping })
        );
        Assert.That(
            audit.Diagnostics,
            Has.None.Matches<AuditDiagnostic>(item =>
                item.Code
                    is SemanticCoverageAudit.UnmappedSurface
                        or SemanticCoverageAudit.DuplicateSurfaceMapping
                        or SemanticCoverageAudit.StaleSurfaceMapping
                        or SemanticCoverageAudit.UnknownFeature
            )
        );
        Assert.That(catalog.SurfaceMappings, Has.Count.EqualTo(inventory.Surfaces.Count));
        Assert.That(
            catalog.SurfaceMappings.Select(mapping => mapping.SurfaceId).Distinct(StringComparer.Ordinal).ToArray(),
            Has.Length.EqualTo(inventory.Surfaces.Count),
            "the checked-in catalog must retain exact-once surface mapping rows"
        );
        Assert.That(
            inventory.Surfaces,
            Has.Count.GreaterThan(1000),
            "the audit must cover the whole inventory, not a slice"
        );

        SemanticInventory live = GraphSemanticCensus.Read(
            root,
            new[] { "SIL.Machine.Morphology.HermitCrab.XmlLanguageLoader" },
            CancellationToken.None
        );
        Assert.That(
            live.Surfaces.Count,
            Is.GreaterThan(inventory.Surfaces.Count),
            "the live snapshot must add C# census surfaces beyond the legacy DTD inventory"
        );
    }

    [Test]
    public void ADroppedCatalogRowFailsTheAudit()
    {
        string root = RepositoryRoot();
        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(root);
        SemanticCatalog catalog = CatalogBootstrap.Load(root);
        var trimmed = catalog with { SurfaceMappings = catalog.SurfaceMappings.Skip(1).ToArray() };

        AuditResult audit = SemanticCoverageAudit.Run(inventory, trimmed);

        Assert.That(
            audit.Diagnostics.Select(item => item.Code),
            Does.Contain(SemanticCoverageAudit.UnmappedSurface),
            "removing a mapping row must fail closed"
        );
    }

    [Test]
    public void LedgerClassificationsAreRecomputedNotTrusted()
    {
        string root = RepositoryRoot();
        GrammarCoverageResult result = GrammarCoverageGate.Compute(root, GrammarCoverageGate.ReadInventory(root));

        var recorded = GrammarCoverageGate
            .ReadBaseline(root)
            .ToDictionary(entry => entry.SurfaceId, entry => entry.Classification, StringComparer.Ordinal);
        var recomputed = GrammarCoverageGate
            .Classify(root, result.Uncovered, GrammarCoverageGate.ReadBaseline(root))
            .ToDictionary(entry => entry.SurfaceId, entry => entry.Classification, StringComparer.Ordinal);

        string[] wrong = recomputed
            .Where(pair => recorded.TryGetValue(pair.Key, out string? was) && was != pair.Value)
            .Select(pair => $"{pair.Key}: recorded '{recorded[pair.Key]}', recomputed '{pair.Value}'")
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();

        Assert.That(
            wrong,
            Is.Empty,
            $"a dead-schema claim is only true while the engine ignores the element:\n  {string.Join("\n  ", wrong)}"
        );
    }

    [Test]
    public void DeadSchemaElementsAreGenuinelyAbsentFromTheEngine()
    {
        string root = RepositoryRoot();
        string[] claimed = GrammarCoverageGate
            .ReadBaseline(root)
            .Where(entry => entry.Classification == GrammarCoverageGate.DeadSchema)
            .Select(entry => DeadSchemaDetector.OwningElement(entry.SurfaceId)!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(claimed, Is.Not.Empty, "the ledger should record the schema the engine ignores");
        Assert.That(
            DeadSchemaDetector.FindUnreferenced(root, claimed).OrderBy(name => name, StringComparer.Ordinal),
            Is.EqualTo(claimed),
            "every element classified dead-schema must have no quoted occurrence in the engine source"
        );
    }

    [Test]
    public void ReferencedElementsAreNotClassifiedDeadSchema()
    {
        string root = RepositoryRoot();

        Assert.That(
            DeadSchemaDetector.FindUnreferenced(root, new[] { "Stratum", "MorphologicalRule", "LexicalEntry" }),
            Is.Empty,
            "elements the loader plainly reads must never be detected as dead schema"
        );
    }

    [Test]
    public void OwningElementIsParsedFromBothObservableSurfaceShapes()
    {
        Assert.That(DeadSchemaDetector.OwningElement("dtd:element/AffixTemplate"), Is.EqualTo("AffixTemplate"));
        Assert.That(DeadSchemaDetector.OwningElement("dtd:enum/AffixTemplate/final/true"), Is.EqualTo("AffixTemplate"));
        Assert.That(DeadSchemaDetector.OwningElement("dtd:attribute/AffixTemplate/final"), Is.Null);
    }

    // VariableFeature names are Greek letters; the inventory percent-encodes every ID component, so
    // reading an attribute value raw silently reports 24 covered values as gaps.
    [Test]
    public void NonAsciiEnumeratedValuesAreMatchedThroughTheSameEncoding()
    {
        string root = RepositoryRoot();
        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(root);
        var grammar = XDocument.Parse(
            "<VariableFeatures><VariableFeature id=\"v\" name=\"α\" phonologicalFeature=\"f\" /></VariableFeatures>"
        );

        Assert.That(GrammarFeatureUsage.Read(grammar, inventory), Does.Contain("dtd:enum/VariableFeature/name/%CE%B1"));
    }

    [Test]
    public void AFixtureCannotClaimASurfaceTheInventoryDoesNotDeclare()
    {
        string root = RepositoryRoot();
        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(root);
        var invented = XDocument.Parse("<NotADeclaredElement notADeclaredAttribute=\"whatever\" />");

        Assert.That(GrammarFeatureUsage.Read(invented, inventory), Is.Empty);
    }
}
