#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>Whether a declared interface is a single <c>IDREF</c> or a list-valued <c>IDREFS</c>.</summary>
public enum InterfaceRefKind
{
    IdRef,
    IdRefs,
}

/// <summary>
/// Whether a declared interface produces a payload (<see cref="Write"/>), gates on one
/// (<see cref="Read"/>), or neither -- a plain structural handoff (<see cref="Ref"/>). See
/// <see cref="InterfaceDirectionClassifier"/> for how this is inferred.
/// </summary>
public enum InterfaceDirection
{
    Write,
    Read,
    Ref,
}

/// <summary>
/// Infers a declared interface's <see cref="InterfaceDirection"/> from its attribute name. This is a
/// naming CONVENTION the DTD's schema itself does not state -- the DTD says only "this attribute
/// points at some ID", never "this attribute writes/reads a payload" -- so the prefixes below are the
/// one place judgment enters this generator, isolated here so it stays reviewable in one spot.
/// </summary>
internal static class InterfaceDirectionClassifier
{
    private static readonly string[] WritePrefixes = { "output", "assigned" };
    private static readonly string[] ReadPrefixes = { "required", "excluded", "obligatory", "head", "nonHead" };

    public static InterfaceDirection Classify(string attributeName)
    {
        ArgumentNullException.ThrowIfNull(attributeName);

        if (attributeName == "MPRFeatures" ||
            WritePrefixes.Any(prefix => attributeName.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return InterfaceDirection.Write;
        }

        if (ReadPrefixes.Any(prefix => attributeName.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return InterfaceDirection.Read;
        }

        return InterfaceDirection.Ref;
    }
}

/// <summary>A payload element type that is both written and read by at least one declared interface each.</summary>
public sealed record InterfaceJunction(string TargetType, int WriterCount, int ReaderCount);

/// <summary>
/// The checked-in denominator for interface coverage: one row per <c>IDREF</c>/<c>IDREFS</c>
/// attribute declared anywhere in <c>HermitCrabInput.dtd</c>, resolved against the real corpus to
/// find which element types it actually points at. Unlike <see cref="RuleInteractionLedger"/> (a
/// corpus statistic that grows with the fixture set), this ledger's denominator is fixed by the DTD
/// alone -- adding a fixture can only change which declared interfaces are marked exercised and what
/// their observed target types are, never how many rows exist.
/// </summary>
public static class InterfaceInventoryLedger
{
    public const string RelativePath = "conformance/interface-inventory.tsv";

    private const int ColumnCount = 6;

    public sealed record Row(
        string Element,
        string Attribute,
        InterfaceRefKind RefKind,
        IReadOnlyList<string> ObservedTargetTypes,
        bool Exercised,
        InterfaceDirection Direction
    );

    /// <summary>
    /// Declares every <c>IDREF</c>/<c>IDREFS</c> attribute in the DTD, then resolves each one against
    /// every fixture's grammar: for every element in the corpus that carries the declared attribute
    /// with a non-empty value, its IDREF(S) tokens are looked up in that fixture's own id-to-element
    /// map, and every element type found is recorded as an observed target type for that interface.
    /// A declared interface with no matching element anywhere in the corpus is unexercised.
    /// </summary>
    public static IReadOnlyList<Row> Compute(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);

        string dtdPath = Path.Combine(
            repositoryRoot,
            GrammarCoverageGate.DtdRelativePath.Replace('/', Path.DirectorySeparatorChar)
        );
        string dtdText = File.ReadAllText(dtdPath);
        IReadOnlyList<(string Element, string Attribute, InterfaceRefKind RefKind)> declared = DeclaredInterfaces(
            GrammarCoverageGate.DtdRelativePath,
            dtdText
        );

        var observedTargets = new Dictionary<(string Element, string Attribute), HashSet<string>>();
        var exercised = new HashSet<(string Element, string Attribute)>();

        foreach (Fixture fixture in Fixture.DiscoverAll(Path.Combine(repositoryRoot, "conformance")))
        {
            XDocument grammar = XDocument.Load(fixture.GrammarPath);
            var idToElement = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (XElement candidate in grammar.Descendants())
            {
                string? id = (string?)candidate.Attribute("id");
                if (id is not null)
                    idToElement[id] = candidate.Name.LocalName;
            }

            foreach ((string element, string attribute, InterfaceRefKind _) in declared)
            {
                foreach (XElement owner in grammar.Descendants(element))
                {
                    string? value = (string?)owner.Attribute(attribute);
                    if (string.IsNullOrEmpty(value))
                        continue;

                    var key = (element, attribute);
                    exercised.Add(key);
                    foreach (string token in SplitIdrefs(value))
                    {
                        if (!idToElement.TryGetValue(token, out string? targetType))
                            continue;

                        if (!observedTargets.TryGetValue(key, out HashSet<string>? targets))
                        {
                            targets = new HashSet<string>(StringComparer.Ordinal);
                            observedTargets[key] = targets;
                        }
                        targets.Add(targetType);
                    }
                }
            }
        }

        var rows = new List<Row>();
        foreach ((string element, string attribute, InterfaceRefKind refKind) in declared)
        {
            var key = (element, attribute);
            IReadOnlyList<string> targets = observedTargets.TryGetValue(key, out HashSet<string>? set)
                ? set.OrderBy(t => t, StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();
            rows.Add(
                new Row(
                    element,
                    attribute,
                    refKind,
                    targets,
                    exercised.Contains(key),
                    InterfaceDirectionClassifier.Classify(attribute)
                )
            );
        }

        return rows
            .OrderBy(r => r.Element, StringComparer.Ordinal)
            .ThenBy(r => r.Attribute, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Groups every observed (element, attribute) -&gt; target-type edge by target type and reports
    /// the payload types that have at least one <see cref="InterfaceDirection.Write"/> edge AND at
    /// least one <see cref="InterfaceDirection.Read"/> edge -- the junctions where a chained
    /// interaction (something writes a payload another construct later reads) is actually possible.
    /// <see cref="InterfaceDirection.Ref"/> edges contribute to neither count.
    /// </summary>
    public static IReadOnlyList<InterfaceJunction> ComputeJunctions(IReadOnlyList<Row> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var writers = new Dictionary<string, int>(StringComparer.Ordinal);
        var readers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Row row in rows)
        {
            if (row.Direction == InterfaceDirection.Ref)
                continue;

            Dictionary<string, int> counts = row.Direction == InterfaceDirection.Write ? writers : readers;
            foreach (string targetType in row.ObservedTargetTypes)
                counts[targetType] = counts.GetValueOrDefault(targetType) + 1;
        }

        return writers
            .Keys.Where(readers.ContainsKey)
            .OrderBy(t => t, StringComparer.Ordinal)
            .Select(t => new InterfaceJunction(t, writers[t], readers[t]))
            .ToArray();
    }

    /// <summary>Every <c>IDREF</c>/<c>IDREFS</c> attribute the DTD declares.</summary>
    private static IReadOnlyList<(string Element, string Attribute, InterfaceRefKind RefKind)> DeclaredInterfaces(
        string dtdPath,
        string dtdText
    )
    {
        SemanticInventory inventory = DtdInventoryReader.Read(dtdPath, dtdText);
        var declared = new List<(string, string, InterfaceRefKind)>();
        foreach (InventorySurface surface in inventory.Surfaces)
        {
            if (surface.Kind != "attribute" || surface.Parent is null)
                continue;

            string? type = ParseDeclaredType(surface.Value);
            if (type == "IDREF")
                declared.Add((surface.Parent, surface.Name, InterfaceRefKind.IdRef));
            else if (type == "IDREFS")
                declared.Add((surface.Parent, surface.Name, InterfaceRefKind.IdRefs));
        }

        return declared;
    }

    // DtdInventoryReader encodes an "attribute" surface's Value as "type=X;default=Y;fixed=Z"; this
    // reads only the leading type field back out.
    private static string? ParseDeclaredType(string? value)
    {
        if (value is null)
            return null;

        foreach (string field in value.Split(';'))
        {
            if (field.StartsWith("type=", StringComparison.Ordinal))
                return field["type=".Length..];
        }

        return null;
    }

    private static string[] SplitIdrefs(string value) => value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    public static void Write(string repositoryRoot, IReadOnlyList<Row> rows)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(rows);
        string path = Path.Combine(repositoryRoot, RelativePath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllText(path, ToText(rows));
    }

    /// <summary>
    /// Renders the ledger deterministically (element, then attribute) so two runs over an unchanged
    /// DTD and corpus byte-for-byte agree -- <c>CheckedInInterfaceInventoryLedgerIsUpToDate</c> depends
    /// on this for its drift check.
    /// </summary>
    public static string ToText(IReadOnlyList<Row> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var writer = new StringWriter();
        writer.WriteLine(
            "# GENERATED by hc-conformance --write-interface-inventory. One row per IDREF/IDREFS attribute"
        );
        writer.WriteLine(
            "# declared anywhere in HermitCrabInput.dtd -- a fixed denominator that moves only when the DTD"
        );
        writer.WriteLine(
            "# changes, never when a fixture is added. observed_target_types is the set of element types the"
        );
        writer.WriteLine(
            "# attribute's IDREF(S) tokens actually resolved to, across every fixture's own id-to-element map;"
        );
        writer.WriteLine(
            "# empty when unexercised. direction (write/read/ref) is inferred from the attribute name by"
        );
        writer.WriteLine(
            "# InterfaceDirectionClassifier -- a naming convention, not something the DTD itself states."
        );
        writer.WriteLine(
            "element\tattribute\tref_kind\tobserved_target_types\texercised\tdirection"
        );
        foreach (Row row in rows.OrderBy(r => r.Element, StringComparer.Ordinal).ThenBy(r => r.Attribute, StringComparer.Ordinal))
        {
            writer.WriteLine(
                string.Join(
                    '\t',
                    row.Element,
                    row.Attribute,
                    row.RefKind,
                    string.Join(",", row.ObservedTargetTypes),
                    row.Exercised ? "yes" : "no",
                    row.Direction
                )
            );
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
                || line.StartsWith("element\t", StringComparison.Ordinal)
            )
            {
                continue;
            }

            string[] fields = line.Split('\t');
            if (fields.Length != ColumnCount)
                throw new FormatException($"{RelativePath}: '{line}' must be {ColumnCount} tab-separated fields");
            if (!Enum.TryParse(fields[2], out InterfaceRefKind refKind))
                throw new FormatException($"{RelativePath}: unknown ref kind '{fields[2]}'");
            if (fields[4] is not ("yes" or "no"))
                throw new FormatException($"{RelativePath}: unknown exercised flag '{fields[4]}'");
            if (!Enum.TryParse(fields[5], out InterfaceDirection direction))
                throw new FormatException($"{RelativePath}: unknown direction '{fields[5]}'");

            IReadOnlyList<string> targets = fields[3].Length == 0
                ? Array.Empty<string>()
                : fields[3].Split(',');
            rows.Add(new Row(fields[0], fields[1], refKind, targets, fields[4] == "yes", direction));
        }

        return rows;
    }
}
