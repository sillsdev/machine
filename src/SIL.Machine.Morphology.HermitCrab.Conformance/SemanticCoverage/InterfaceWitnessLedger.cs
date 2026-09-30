#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// The checked-in denominator for interface WITNESS (as opposed to
/// <see cref="InterfaceInventoryLedger"/>'s presence): one row per (element, attribute, fixture) triple
/// where <see cref="InterfaceInventoryLedger"/> already recorded the interface as present in that
/// fixture, with the severance verdict <see cref="InterfaceWitnessGate.Evaluate"/> reaches for it.
/// Presence only means the attribute resolves to a real IDREF somewhere; it is silent on whether
/// severing it would ever change a parse. This ledger is the machine-checked answer to that question,
/// run the same way <see cref="CounterfactualLedger"/> already answers it for a unit surface.
/// </summary>
public static class InterfaceWitnessLedger
{
    public const string RelativePath = "conformance/interface-witness.tsv";

    private const string NullField = "-";
    private const int ColumnCount = 10;

    /// <summary>
    /// Runs <see cref="InterfaceWitnessGate.Evaluate"/> for every (element, attribute, fixture) triple
    /// <see cref="InterfaceInventoryLedger"/> marks present. Baseline is computed once per fixture,
    /// through the same killable child process the mutant uses (see
    /// <see cref="InterfaceWitnessGate.Evaluate"/>'s own doc comment for why neither run here gets an
    /// unprotected path, unlike a unit-surface neutralization).
    /// </summary>
    public static IReadOnlyList<InterfaceWitnessResult> Sweep(
        string repositoryRoot,
        Action<string, int>? onFixtureStarted = null,
        Action<string, long>? onWordTimed = null
    )
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(repositoryRoot);
        IReadOnlyList<InterfaceInventoryLedger.Row> declared = InterfaceInventoryLedger.Compute(repositoryRoot);

        var byFixture = new Dictionary<string, List<(string Element, string Attribute)>>(StringComparer.Ordinal);
        foreach (InterfaceInventoryLedger.Row row in declared)
        {
            if (!row.Present)
                continue;
            foreach (string fixtureId in row.Fixtures)
            {
                if (!byFixture.TryGetValue(fixtureId, out List<(string, string)>? interfaces))
                {
                    interfaces = new List<(string, string)>();
                    byFixture[fixtureId] = interfaces;
                }
                interfaces.Add((row.Element, row.Attribute));
            }
        }

        string scratch = Path.Combine(Path.GetTempPath(), "hc-interface-witness");
        var results = new List<InterfaceWitnessResult>();
        foreach (Fixture fixture in Fixture.DiscoverAll(Path.Combine(repositoryRoot, "conformance")))
        {
            if (!byFixture.TryGetValue(fixture.Id, out List<(string Element, string Attribute)>? interfaces))
                continue;

            onFixtureStarted?.Invoke(fixture.Id, interfaces.Count);
            string[] words = fixture.Words.Words.Select(word => word.Word).ToArray();

            IReadOnlyList<string> baseline;
            try
            {
                baseline = CounterfactualGate.EvaluateWithTimeout(
                    fixture.GrammarPath,
                    words,
                    onTimed: (word, ms) => onWordTimed?.Invoke(word, ms)
                );
            }
            catch (TimeoutException)
            {
                foreach ((string element, string attribute) in interfaces)
                {
                    results.Add(
                        new InterfaceWitnessResult(
                            element,
                            attribute,
                            fixture.Id,
                            CounterfactualVerdict.Timeout,
                            "none",
                            "the fixture's own unmutated baseline did not terminate within the timeout"
                        )
                    );
                }
                continue;
            }
            catch (Exception ex)
            {
                foreach ((string element, string attribute) in interfaces)
                {
                    results.Add(
                        new InterfaceWitnessResult(
                            element,
                            attribute,
                            fixture.Id,
                            CounterfactualVerdict.Unobservable,
                            "none",
                            $"the fixture's own unmutated baseline failed to evaluate: {ex.GetType().Name}"
                        )
                    );
                }
                continue;
            }

            foreach ((string element, string attribute) in interfaces)
            {
                results.Add(
                    InterfaceWitnessGate.Evaluate(
                        fixture,
                        element,
                        attribute,
                        inventory,
                        baseline,
                        scratch,
                        onWordTimed: (word, ms) => onWordTimed?.Invoke(word, ms)
                    )
                );
            }
        }

        return results
            .OrderBy(r => r.Element, StringComparer.Ordinal)
            .ThenBy(r => r.Attribute, StringComparer.Ordinal)
            .ThenBy(r => r.FixtureId, StringComparer.Ordinal)
            .ToArray();
    }

    public static void Write(string repositoryRoot, IReadOnlyList<InterfaceWitnessResult> rows)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(rows);
        string path = Path.Combine(repositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllText(path, ToText(rows));
    }

    /// <summary>
    /// Renders the ledger deterministically (element, attribute, fixture) so two runs over an
    /// unchanged DTD and corpus byte-for-byte agree -- the same drift-check contract every other ledger
    /// in this directory follows.
    /// </summary>
    public static string ToText(IReadOnlyList<InterfaceWitnessResult> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var writer = new StringWriter();
        writer.WriteLine(
            "# GENERATED by hc-conformance --write-coverage-traceability. One row per (element, attribute,"
        );
        writer.WriteLine(
            "# fixture) triple InterfaceInventoryLedger marks present -- i.e. one severance run per place the"
        );
        writer.WriteLine("# interface actually shows up. Presence (interface-inventory.tsv) only means the attribute");
        writer.WriteLine(
            "# resolves to a real IDREF somewhere; it says nothing about whether the interface DOES anything."
        );
        writer.WriteLine(
            "# This ledger answers that by emptying the attribute out of every occurrence in the fixture and"
        );
        writer.WriteLine("# re-parsing every word against the mutant, exactly as CounterfactualLedger does for a unit");
        writer.WriteLine("# surface. verdict is CounterfactualVerdict: only Evidenced is a word-level witness;");
        writer.WriteLine(
            "# RequiredByLoader/RequiredByDtd mean severing it made the grammar fail to load (a real but weaker"
        );
        writer.WriteLine(
            "# signal -- no word was ever reached); Unobservable means the interface is present but INERT in"
        );
        writer.WriteLine("# this fixture -- every word parsed identically with it severed. Absent fields are \"-\".");
        writer.WriteLine(
            "element\tattribute\tfixture\tverdict\tmutation\tdelta\texample_word\texample_outcome\tcounterexample_kind\tcounterexample_outcome"
        );
        foreach (
            InterfaceWitnessResult row in rows.OrderBy(r => r.Element, StringComparer.Ordinal)
                .ThenBy(r => r.Attribute, StringComparer.Ordinal)
                .ThenBy(r => r.FixtureId, StringComparer.Ordinal)
        )
        {
            writer.WriteLine(
                string.Join(
                    '\t',
                    row.Element,
                    row.Attribute,
                    row.FixtureId,
                    row.Verdict,
                    row.Mutation,
                    row.Delta,
                    row.ExampleWord ?? NullField,
                    row.ExampleOutcome ?? NullField,
                    row.CounterexampleKind,
                    row.CounterexampleOutcome ?? NullField
                )
            );
        }
        return writer.ToString();
    }

    /// <summary>Reads the checked-in ledger, or an empty list if it has never been written.</summary>
    public static IReadOnlyList<InterfaceWitnessResult> Read(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        string path = Path.Combine(repositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
            return Array.Empty<InterfaceWitnessResult>();

        var rows = new List<InterfaceWitnessResult>();
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (
                line.Length == 0
                || line.StartsWith("#", StringComparison.Ordinal)
                || line.StartsWith("element\t", StringComparison.Ordinal)
            )
            {
                continue;
            }

            string[] fields = line.Split('\t');
            if (fields.Length != ColumnCount)
                throw new FormatException($"{RelativePath}: '{line}' must be {ColumnCount} tab-separated fields");
            if (!Enum.TryParse(fields[3], out CounterfactualVerdict verdict))
                throw new FormatException($"{RelativePath}: unknown verdict '{fields[3]}'");
            if (!Enum.TryParse(fields[8], out CounterexampleKind counterexampleKind))
                throw new FormatException($"{RelativePath}: unknown counterexample kind '{fields[8]}'");

            rows.Add(
                new InterfaceWitnessResult(
                    fields[0],
                    fields[1],
                    fields[2],
                    verdict,
                    fields[4],
                    fields[5],
                    fields[6] == NullField ? null : fields[6],
                    fields[7] == NullField ? null : fields[7],
                    counterexampleKind,
                    fields[9] == NullField ? null : fields[9]
                )
            );
        }

        return rows;
    }
}
