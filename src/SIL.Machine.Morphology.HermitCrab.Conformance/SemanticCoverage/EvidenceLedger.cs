#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// The machine-checkable coverage ledger: one row per <see cref="CoverageItem"/> resolved by
/// <see cref="Evidence"/>, with the example, the counter-example, and its kind as explicit columns.
/// Unlike <see cref="CounterfactualLedger"/>'s free-text Delta column, every field here is read back
/// and compared by <see cref="CoverageCompletenessGate"/> -- it is the source of truth, not
/// documentation of one. Items resolved by <see cref="Proof"/> instead live in
/// <see cref="ImpossibilityProofs"/>'s file, never here.
/// </summary>
public static class EvidenceLedger
{
    public const string RelativePath = "conformance/semantic-coverage-evidence.tsv";

    private const string NullField = "-";
    private const int ColumnCount = 9;

    /// <summary>One ledger line: a <see cref="CoverageItem"/> joined to the <see cref="Evidence"/> that
    /// resolves it.</summary>
    public sealed record Row(
        string ItemId,
        CoverageItemKind Kind,
        string Fixture,
        string? ExampleWord,
        string? ExampleOutcome,
        CounterexampleKind CounterexampleKind,
        string? CounterexampleOutcome,
        string Mutation,
        CounterfactualVerdict Verdict
    );

    /// <summary>Joins an item to its evidence into the flattened row the ledger stores.</summary>
    public static Row ToRow(CoverageItem item, Evidence evidence)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(evidence);
        if (item.Id != evidence.ItemId)
            throw new ArgumentException($"item '{item.Id}' does not match evidence for '{evidence.ItemId}'");

        return new Row(
            item.Id,
            item.Kind,
            evidence.Fixture,
            evidence.ExampleWord,
            evidence.ExampleOutcome,
            evidence.CounterexampleKind,
            evidence.CounterexampleOutcome,
            evidence.Mutation,
            evidence.Verdict
        );
    }

    /// <summary>Restores evidence only after checking the row against its generated item.</summary>
    public static Evidence ToEvidence(Row row, CoverageItem item)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(item);
        if (!string.Equals(row.ItemId, item.Id, StringComparison.Ordinal))
            throw new ArgumentException($"evidence row item '{row.ItemId}' does not match generated item '{item.Id}'", nameof(row));
        if (row.Kind != item.Kind)
            throw new ArgumentException($"evidence row '{row.ItemId}' kind '{row.Kind}' does not match generated kind '{item.Kind}'", nameof(row));
        if (!string.Equals(row.Fixture, item.Fixture, StringComparison.Ordinal))
            throw new ArgumentException($"evidence row '{row.ItemId}' fixture '{row.Fixture}' does not match generated fixture '{item.Fixture}'", nameof(row));

        return new Evidence(
            row.ItemId,
            row.Fixture,
            row.ExampleWord,
            row.ExampleOutcome,
            row.CounterexampleKind,
            row.CounterexampleOutcome,
            row.Mutation,
            row.Verdict
        );
    }

    public static void Write(string repositoryRoot, IReadOnlyList<Row> rows)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(rows);
        string path = Path.Combine(repositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        using var writer = new StreamWriter(path, false);
        writer.WriteLine("# GENERATED. One line per coverage item resolved by evidence -- an item resolved by");
        writer.WriteLine("# proof instead lives in semantic-coverage-proofs.tsv, never here. Columns are");
        writer.WriteLine("# explicit so CoverageCompletenessGate reads structure, never Delta's free-text prose.");
        writer.WriteLine("# counterexample_kind is Word or LoadFailure and the two are never summed into one");
        writer.WriteLine("# count -- see CoverageCompletenessGate. Absent fields are \"-\".");
        writer.WriteLine(
            "item_id\tkind\tfixture\texample_word\texample_outcome\tcounterexample_kind\tcounterexample_outcome\tmutation\tverdict"
        );
        foreach (Row row in rows.OrderBy(r => r.ItemId, StringComparer.Ordinal))
        {
            writer.WriteLine(
                string.Join(
                    '\t',
                    row.ItemId,
                    row.Kind,
                    row.Fixture,
                    row.ExampleWord ?? NullField,
                    row.ExampleOutcome ?? NullField,
                    row.CounterexampleKind,
                    row.CounterexampleOutcome ?? NullField,
                    row.Mutation,
                    row.Verdict
                )
            );
        }
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
                || line.StartsWith("item_id\t", StringComparison.Ordinal)
            )
            {
                continue;
            }

            string[] fields = line.Split('\t');
            if (fields.Length != ColumnCount)
                throw new FormatException($"{RelativePath}: '{line}' must be {ColumnCount} tab-separated fields");
            if (!Enum.TryParse(fields[1], out CoverageItemKind kind))
                throw new FormatException($"{RelativePath}: unknown kind '{fields[1]}' for '{fields[0]}'");
            if (!Enum.TryParse(fields[5], out CounterexampleKind counterexampleKind))
                throw new FormatException($"{RelativePath}: unknown counterexample kind '{fields[5]}' for '{fields[0]}'");
            if (!Enum.TryParse(fields[8], out CounterfactualVerdict verdict))
                throw new FormatException($"{RelativePath}: unknown verdict '{fields[8]}' for '{fields[0]}'");

            rows.Add(
                new Row(
                    fields[0],
                    kind,
                    fields[2],
                    fields[3] == NullField ? null : fields[3],
                    fields[4] == NullField ? null : fields[4],
                    counterexampleKind,
                    fields[6] == NullField ? null : fields[6],
                    fields[7],
                    verdict
                )
            );
        }

        return rows.OrderBy(row => row.ItemId, StringComparer.Ordinal).ToArray();
    }
}
