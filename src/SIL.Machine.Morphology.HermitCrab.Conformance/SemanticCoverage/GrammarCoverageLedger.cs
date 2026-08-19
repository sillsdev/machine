#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>Which of the three layers <see cref="GrammarCoverageLedger"/> reports an obligation from.
/// A fourth layer -- chains/junctions -- is out of scope here; it is owned by a separate generator
/// (<c>conformance/interaction-chains.tsv</c>).</summary>
public enum ObligationLayer
{
    Surface,
    Interface,
    Construct,
}

/// <summary>
/// Per-fixture rollup: for each of the 33 fixtures, which obligations it witnesses, joined from the
/// three already-checked-in ledgers rather than recomputed -- <see cref="EvidenceLedger"/> (unit
/// surfaces), <see cref="InterfaceInventoryLedger"/> + <see cref="InterfaceWitnessLedger"/> (interface
/// edges), and <see cref="ConstructClaimCorroboration"/> (the coarse, self-reported constructs).
/// Deliberately cheap: every source it reads is already a file on disk, so this answers "what would I
/// lose if I deleted this fixture" without a single re-parse of its own -- the expensive part
/// (severance sweeps) already happened when those ledgers were generated.
/// </summary>
public static class GrammarCoverageLedger
{
    public const string RelativePath = "conformance/grammar-coverage-ledger.tsv";

    private const int ColumnCount = 5;

    public sealed record Row(string Fixture, ObligationLayer Layer, string Obligation, string Status, string Detail);

    private static string SurfaceStatus(CounterfactualVerdict verdict) =>
        verdict switch
        {
            CounterfactualVerdict.Evidenced or CounterfactualVerdict.EvidencedJointly => "witnessed",
            CounterfactualVerdict.RequiredByLoader => "required-by-loader",
            CounterfactualVerdict.RequiredByDtd => "required-by-dtd",
            CounterfactualVerdict.Timeout => "timeout",
            _ => "unobservable",
        };

    private static string InterfaceStatus(CounterfactualVerdict verdict) =>
        verdict switch
        {
            CounterfactualVerdict.Evidenced => "witnessed",
            CounterfactualVerdict.RequiredByLoader => "required-by-loader",
            CounterfactualVerdict.RequiredByDtd => "required-by-dtd",
            CounterfactualVerdict.Timeout => "timeout",
            _ => "present-but-inert",
        };

    private static string ConstructStatus(ConstructClaimStatus status) =>
        status switch
        {
            ConstructClaimStatus.Confirmed => "claimed-confirmed",
            ConstructClaimStatus.Contradicted => "claimed-contradicted",
            _ => "claimed-unmapped",
        };

    // Badness order for rolling several claims of the same (fixture, construct) up into one status:
    // a single Contradicted sibling is a real problem even if nine others are Confirmed, and an
    // Unmapped sibling is a weaker, more neutral gap than either -- neither is ConstructClaimStatus's
    // own declaration order (Confirmed, Contradicted, Unmapped), so this cannot be a bare Min().
    private static int Badness(ConstructClaimStatus status) =>
        status switch
        {
            ConstructClaimStatus.Contradicted => 0,
            ConstructClaimStatus.Unmapped => 1,
            _ => 2,
        };

    public static IReadOnlyList<Row> Compute(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        var rows = new List<Row>();

        foreach (EvidenceLedger.Row item in EvidenceLedger.Read(repositoryRoot))
        {
            if (item.Kind != CoverageItemKind.Surface)
                continue;
            rows.Add(
                new Row(item.Fixture, ObligationLayer.Surface, item.ItemId, SurfaceStatus(item.Verdict), $"{item.Verdict}")
            );
        }

        var witnessByKey = InterfaceWitnessLedger
            .Read(repositoryRoot)
            .ToDictionary(w => (w.Element, w.Attribute, w.FixtureId), w => w);
        foreach (InterfaceInventoryLedger.Row declared in InterfaceInventoryLedger.Read(repositoryRoot))
        {
            if (!declared.Present)
                continue;
            foreach (string fixtureId in declared.Fixtures)
            {
                string obligation = $"{declared.Element}.{declared.Attribute}";
                if (witnessByKey.TryGetValue((declared.Element, declared.Attribute, fixtureId), out InterfaceWitnessResult? witness))
                {
                    rows.Add(
                        new Row(fixtureId, ObligationLayer.Interface, obligation, InterfaceStatus(witness.Verdict), $"{witness.Verdict}")
                    );
                }
                else
                {
                    rows.Add(
                        new Row(fixtureId, ObligationLayer.Interface, obligation, "witness-not-yet-computed", "-")
                    );
                }
            }
        }

        foreach (
            var group in ConstructClaimCorroboration
                .Read(repositoryRoot)
                .GroupBy(claim => (claim.Fixture, claim.Construct))
        )
        {
            // A construct can be claimed by several words in the same fixture; the fixture-level fact
            // (does the grammar contain what the text names) is the same for all of them, so the worst
            // status among the group is the honest one -- one contradicted claim is a real problem even
            // if nine siblings are confirmed for the identical construct.
            ConstructClaimStatus worst = group.OrderBy(claim => Badness(claim.Status)).First().Status;
            rows.Add(
                new Row(
                    group.Key.Fixture,
                    ObligationLayer.Construct,
                    group.Key.Construct,
                    ConstructStatus(worst),
                    $"{group.Count()} claim(s)"
                )
            );
        }

        return rows
            .OrderBy(r => r.Fixture, StringComparer.Ordinal)
            .ThenBy(r => r.Layer)
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
        writer.WriteLine("# GENERATED by hc-conformance --write-coverage-traceability. One row per (fixture,");
        writer.WriteLine("# obligation) pair this fixture witnesses, joined from three already-checked-in ledgers --");
        writer.WriteLine("# never recomputed here, so this file costs no re-parse of its own. layer is Surface");
        writer.WriteLine("# (semantic-coverage-evidence.tsv), Interface (interface-inventory.tsv +");
        writer.WriteLine("# interface-witness.tsv), or Construct (construct-claim-corroboration.tsv); the chain/");
        writer.WriteLine("# junction layer is out of scope (owned by interaction-chains.tsv's own generator).");
        writer.WriteLine("# status vocabulary differs per layer -- see GrammarCoverageLedger's *Status methods --");
        writer.WriteLine("# but 'witnessed' always means the same thing: a word-level counterfactual delta, never");
        writer.WriteLine("# mere presence. detail carries the underlying verdict/claim-count for that reason.");
        writer.WriteLine("fixture\tlayer\tobligation\tstatus\tdetail");
        foreach (
            Row row in rows
                .OrderBy(r => r.Fixture, StringComparer.Ordinal)
                .ThenBy(r => r.Layer)
                .ThenBy(r => r.Obligation, StringComparer.Ordinal)
        )
        {
            writer.WriteLine(string.Join('\t', row.Fixture, row.Layer, row.Obligation, row.Status, row.Detail));
        }
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
                || line.StartsWith("fixture\t", StringComparison.Ordinal)
            )
            {
                continue;
            }

            string[] fields = line.Split('\t');
            if (fields.Length != ColumnCount)
                throw new FormatException($"{RelativePath}: '{line}' must be {ColumnCount} tab-separated fields");
            if (!Enum.TryParse(fields[1], out ObligationLayer layer))
                throw new FormatException($"{RelativePath}: unknown layer '{fields[1]}'");

            rows.Add(new Row(fields[0], layer, fields[2], fields[3], fields[4]));
        }

        return rows;
    }
}
