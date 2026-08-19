#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Which obligations a language-family grammar (<c>conformance/languages/*</c>) could pick up by
/// growing an existing fixture, rather than by authoring a new fabricated one -- the direct input to
/// that fold-in work, not a footnote of it. Two independent layers, each keyed on WITNESS (a real
/// word-level counterfactual delta), never mere presence -- see <see cref="InterfaceInventoryLedger"/>
/// and <see cref="EvidenceLedger"/>'s own doc comments for why presence alone would overclaim here.
/// </summary>
public enum FoldInCategory
{
    /// <summary>Every fixture that witnesses this obligation is an edge-case; no language fixture
    /// witnesses it, and (surface layer only) no language fixture's grammar even structurally contains
    /// it, so folding it in needs new grammar content, not just new words.</summary>
    EdgeCaseOnly,

    /// <summary>
    /// No language fixture reaches the same verdict as the recorded evidence (<see
    /// cref="CounterfactualLedger"/>'s <c>witnessed_by</c> column, not just EvidenceLedger's single
    /// recorded fixture -- a language fixture that only TIES is still a real witness and rules this
    /// category out; see <see cref="CounterfactualLedger.Sweep"/>), but at least one language fixture's
    /// grammar.xml already structurally contains the surface (<see cref="GrammarFeatureUsage"/>). A
    /// weaker, cheaper fold-in target: extending that language fixture's WORDS may be enough, with no
    /// new grammar content required.
    /// </summary>
    PresentInLanguageGrammarAlready,

    /// <summary>Present somewhere in the corpus but never reaches a word-level witness anywhere --
    /// not a fold-in candidate (there is no existing evidence to relocate), but worth naming: presence
    /// alone was exactly the overclaim this whole ledger set exists to stop repeating.</summary>
    NeverWitnessed,
}

/// <summary>
/// Computes <see cref="FoldInCategory"/> for the interface layer (from
/// <see cref="InterfaceInventoryLedger"/> + <see cref="InterfaceWitnessLedger"/>) and the unit-surface
/// layer (from <see cref="EvidenceLedger"/> + <see cref="GrammarFeatureUsage"/>). Cheap: every source
/// is either an already-checked-in ledger or a structural (no-reparse) grammar scan.
/// </summary>
public static class FoldInCandidateLedger
{
    public const string RelativePath = "conformance/fold-in-candidates.tsv";

    private const int ColumnCount = 4;

    public sealed record Row(ObligationLayer Layer, string Obligation, FoldInCategory Category, string Detail);

    private const string LanguagesPrefix = "languages/";

    private static IReadOnlyList<Row> ComputeInterfaceLayer(string repositoryRoot)
    {
        var rows = new List<Row>();
        IReadOnlyList<InterfaceWitnessResult> witnesses = InterfaceWitnessLedger.Read(repositoryRoot);
        foreach (InterfaceInventoryLedger.Row declared in InterfaceInventoryLedger.Read(repositoryRoot))
        {
            if (!declared.Present)
                continue;

            string obligation = $"{declared.Element}.{declared.Attribute}";
            string[] evidencedFixtures = witnesses
                .Where(w => w.Element == declared.Element && w.Attribute == declared.Attribute)
                .Where(w => w.Verdict == CounterfactualVerdict.Evidenced)
                .Select(w => w.FixtureId)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            if (evidencedFixtures.Length == 0)
            {
                rows.Add(
                    new Row(
                        ObligationLayer.Interface,
                        obligation,
                        FoldInCategory.NeverWitnessed,
                        $"present in {declared.Fixtures.Count} fixture(s), witnessed by none"
                    )
                );
                continue;
            }

            bool onlyEdgeCases = evidencedFixtures.All(id => !id.StartsWith(LanguagesPrefix, StringComparison.Ordinal));
            if (onlyEdgeCases)
            {
                rows.Add(
                    new Row(
                        ObligationLayer.Interface,
                        obligation,
                        FoldInCategory.EdgeCaseOnly,
                        $"witnessed only by: {string.Join(",", evidencedFixtures)}"
                    )
                );
            }
        }

        return rows;
    }

    private static IReadOnlyList<Row> ComputeSurfaceLayer(string repositoryRoot)
    {
        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(repositoryRoot);
        var languageSurfaces = new HashSet<string>(StringComparer.Ordinal);
        foreach (Fixture fixture in Fixture.DiscoverAll(Path.Combine(repositoryRoot, "conformance")))
        {
            if (!fixture.Id.StartsWith(LanguagesPrefix, StringComparison.Ordinal))
                continue;
            XDocument grammar = XDocument.Load(fixture.GrammarPath);
            foreach (string surfaceId in GrammarFeatureUsage.Read(grammar, inventory))
                languageSurfaces.Add(surfaceId);
        }

        // Keyed on EVERY fixture that reached the recorded verdict, not just EvidenceLedger's single
        // recorded fixture: CounterfactualLedger.Sweep keeps only the first-discovered fixture per
        // surface as EvidenceLedger's row, and discovery order sorts edge-cases before languages/*, so
        // a language fixture that merely TIES for the same verdict would otherwise be invisible here --
        // exactly the defect that made 21 of 30 recorded fold-in candidates wrong.
        var witnessesBySurface = CounterfactualLedger
            .Read(repositoryRoot)
            .ToDictionary(r => r.SurfaceId, r => (IReadOnlyList<string>)(r.WitnessingFixtures ?? new[] { r.FixtureId }), StringComparer.Ordinal);

        var rows = new List<Row>();
        foreach (EvidenceLedger.Row item in EvidenceLedger.Read(repositoryRoot))
        {
            if (item.Kind != CoverageItemKind.Surface)
                continue;
            if (item.Verdict is not (CounterfactualVerdict.Evidenced or CounterfactualVerdict.EvidencedJointly))
                continue;

            IReadOnlyList<string> witnesses = witnessesBySurface.TryGetValue(item.ItemId, out IReadOnlyList<string>? recorded)
                ? recorded
                : new[] { item.Fixture };
            if (witnesses.Any(fixtureId => fixtureId.StartsWith(LanguagesPrefix, StringComparison.Ordinal)))
                continue; // a language grammar already reaches this same verdict; not a fold-in candidate

            bool structurallyPresentElsewhere = languageSurfaces.Contains(item.ItemId);
            rows.Add(
                structurallyPresentElsewhere
                    ? new Row(
                        ObligationLayer.Surface,
                        item.ItemId,
                        FoldInCategory.PresentInLanguageGrammarAlready,
                        $"recorded best evidence: {item.Fixture}; also structurally present in a languages/* grammar"
                    )
                    : new Row(
                        ObligationLayer.Surface,
                        item.ItemId,
                        FoldInCategory.EdgeCaseOnly,
                        $"recorded best evidence: {item.Fixture}; no languages/* grammar contains this surface at all"
                    )
            );
        }

        return rows;
    }

    public static IReadOnlyList<Row> Compute(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        return ComputeInterfaceLayer(repositoryRoot)
            .Concat(ComputeSurfaceLayer(repositoryRoot))
            .OrderBy(r => r.Layer)
            .ThenBy(r => r.Obligation, StringComparer.Ordinal)
            .ToArray();
    }

    public static void Write(string repositoryRoot, IReadOnlyList<Row> rows)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(rows);
        string path = Path.Combine(repositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllText(path, ToText(rows));
    }

    public static string ToText(IReadOnlyList<Row> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var writer = new StringWriter();
        writer.WriteLine("# GENERATED by hc-conformance --write-coverage-traceability. Obligations a language-");
        writer.WriteLine("# family grammar could pick up by growing an existing fixture, keyed on WITNESS (a real");
        writer.WriteLine("# word-level counterfactual delta) never mere presence -- see FoldInCategory's own doc");
        writer.WriteLine("# comments for exactly what each category claims and does not claim.");
        writer.WriteLine("#   EdgeCaseOnly                     only a fabricated edge case witnesses this; folding");
        writer.WriteLine("#                                    it in needs new grammar content");
        writer.WriteLine("#   PresentInLanguageGrammarAlready  surface layer only: a language grammar already");
        writer.WriteLine("#                                    structurally contains it; new WORDS may be enough");
        writer.WriteLine("#   NeverWitnessed                   interface layer only: present somewhere, witnessed");
        writer.WriteLine("#                                    nowhere -- not a fold-in target, a named gap");
        writer.WriteLine("layer\tobligation\tcategory\tdetail");
        foreach (Row row in rows.OrderBy(r => r.Layer).ThenBy(r => r.Obligation, StringComparer.Ordinal))
            writer.WriteLine(string.Join('\t', row.Layer, row.Obligation, row.Category, row.Detail));
        return writer.ToString();
    }

    /// <summary>Reads the checked-in ledger, or an empty list if it has never been written.</summary>
    public static IReadOnlyList<Row> Read(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        string path = Path.Combine(repositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            return Array.Empty<Row>();

        var rows = new List<Row>();
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (
                line.Length == 0
                || line.StartsWith("#", StringComparison.Ordinal)
                || line.StartsWith("layer\t", StringComparison.Ordinal)
            )
            {
                continue;
            }

            string[] fields = line.Split('\t');
            if (fields.Length != ColumnCount)
                throw new FormatException($"{RelativePath}: '{line}' must be {ColumnCount} tab-separated fields");
            if (!Enum.TryParse(fields[0], out ObligationLayer layer))
                throw new FormatException($"{RelativePath}: unknown layer '{fields[0]}'");
            if (!Enum.TryParse(fields[2], out FoldInCategory category))
                throw new FormatException($"{RelativePath}: unknown category '{fields[2]}'");

            rows.Add(new Row(layer, fields[1], category, fields[3]));
        }

        return rows;
    }
}
