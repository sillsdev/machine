#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Recomputes the two mechanical halves of a <see cref="ImpossibilityProofs.NotInSignature"/> claim:
/// the parse-comparison signature's own source never reads the property, and neutralizing the owning
/// element changes no word's outcome in any fixture that declares one. Neither half alone is a full
/// proof that no code anywhere ever branches on the property's VALUE -- that would need whole-engine
/// dataflow analysis this does not attempt. What this DOES establish mechanically: the specific string
/// two parses are diffed by cannot depend on the property (a source-text fact, re-read every run), and
/// removing the property from every fixture that has one is observably a no-op (an evidence-shaped
/// fact, re-run every run). The remaining gap -- "and no OTHER code path anywhere reads it either" --
/// is a human reading of every call site the property's name appears at, recorded as prose in the
/// proof's checked-in evidence rather than computed here.
/// </summary>
public static class NotInSignatureCheck
{
    public const string SignatureSourceRelativePath = "src/SIL.Machine.Morphology.HermitCrab.Tool/SignatureFormat.cs";

    /// <summary>
    /// True when the file that builds the parse-comparison signature contains no reference to
    /// <paramref name="propertyName"/>. A literal source-text scan, not a semantic one: it cannot see a
    /// reference reached through an alias or reflection, but it does mean a future edit that starts
    /// reading the property directly in <c>BuildSignature</c> is caught the next time this runs.
    /// </summary>
    public static bool SignatureSourceNeverReads(string repositoryRoot, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(propertyName);
        string path = Path.Combine(repositoryRoot, SignatureSourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
        string source = File.ReadAllText(path);
        return !source.Contains($".{propertyName}", StringComparison.Ordinal);
    }

    /// <summary>Every fixture whose grammar document declares at least one <paramref name="elementName"/>.</summary>
    public static IReadOnlyList<string> FixturesContaining(string repositoryRoot, string elementName)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(elementName);
        return GrammarCoverageGate
            .DiscoverGrammars(repositoryRoot)
            .Where(pair => XDocument.Load(pair.GrammarPath).Descendants(elementName).Any())
            .Select(pair => pair.FixtureId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Re-runs the counterfactual for <paramref name="surfaceId"/> against every fixture
    /// <see cref="FixturesContaining"/> finds for <paramref name="elementName"/>, requiring the
    /// mutation to be genuinely applied (never a fixture <see cref="GrammarMutator.Mutate"/> reports as
    /// not containing the surface at all) AND every one of that fixture's words to come back unchanged.
    /// False if no fixture declares the element -- an untested claim is not evidence of anything.
    /// </summary>
    public static bool MutationChangesNoWordInAnyContainingFixture(
        string repositoryRoot,
        string surfaceId,
        string elementName,
        string scratchDirectory
    )
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(surfaceId);
        ArgumentNullException.ThrowIfNull(elementName);
        ArgumentNullException.ThrowIfNull(scratchDirectory);

        IReadOnlyList<string> fixtureIds = FixturesContaining(repositoryRoot, elementName);
        if (fixtureIds.Count == 0)
            return false;

        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(repositoryRoot);
        IReadOnlyList<Fixture> allFixtures = Fixture.DiscoverAll(Path.Combine(repositoryRoot, "conformance"));
        foreach (string fixtureId in fixtureIds)
        {
            Fixture fixture = allFixtures.Single(f => f.Id == fixtureId);
            IReadOnlyList<string> baseline = CounterfactualGate.ComputeBaseline(fixture);
            CounterfactualResult result = CounterfactualGate.Evaluate(fixture, surfaceId, inventory, baseline, scratchDirectory);

            // "none" means nothing was actually removed, so this fixture would test nothing.
            if (result.Mutation == "none")
                return false;
            if (result.Verdict != CounterfactualVerdict.Unobservable)
                return false;
        }

        return true;
    }
}
