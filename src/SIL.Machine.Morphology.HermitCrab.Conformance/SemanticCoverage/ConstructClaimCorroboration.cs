#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Whether a word's/parse's self-reported <c>exercises:</c> claim (a free-text string drawn from
/// <c>conformance/constructs.txt</c>) can be cross-checked against the fixture's own grammar.xml.
/// This is a STRUCTURAL check only -- like <see cref="InterfaceInventoryLedger.Row.Present"/>, not
/// like <see cref="InterfaceWitnessLedger"/> -- so a "Confirmed" claim means the named DTD
/// element/attribute is genuinely present in this fixture's grammar, never that the word's parse
/// actually depends on it.
/// </summary>
public enum ConstructClaimStatus
{
    /// <summary>At least one DTD identifier mapped from the construct's text is present in this
    /// fixture's grammar.xml.</summary>
    Confirmed,

    /// <summary>DTD identifiers were mapped from the construct's text, but none of them appear
    /// anywhere in this fixture's grammar.xml -- the claim looks wrong.</summary>
    Contradicted,

    /// <summary>The construct's text contains no identifier this mapping can recognize, so the claim
    /// cannot be mechanically checked at all (neither confirmed nor contradicted).</summary>
    Unmapped,
}

/// <summary>
/// Cross-checks every <c>exercises:</c> claim in every fixture's <c>words.yaml</c> against that
/// fixture's own <c>grammar.xml</c>. <see cref="MapConstructsToDtdTokens"/> is the one place judgment
/// enters this generator: a construct string in <c>constructs.txt</c> is free-text prose, not a DTD
/// identifier, so recognizing which DTD element/attribute names it names has to be a text match, not
/// a lookup. Two conservative rules keep that match from just re-finding ordinary English words:
/// element names are matched case-SENSITIVELY (the DTD always capitalizes them, and construct prose
/// does too, so an accidental match would require the exact PascalCase spelling); attribute names are
/// matched only when they contain an internal capital (a real multi-word compound like
/// <c>isActive</c> or <c>requiredStemName</c>) -- a bare single-word attribute like <c>type</c>,
/// <c>name</c>, or <c>feature</c> is excluded outright, because those words appear in ordinary English
/// prose often enough that "confirmed" would stop meaning anything.
/// </summary>
public static class ConstructClaimCorroboration
{
    public const string RelativePath = "conformance/construct-claim-corroboration.tsv";

    private const string NullField = "-";
    private const int ColumnCount = 6;

    public sealed record Row(
        string Fixture,
        string Word,
        string Signature,
        string Construct,
        ConstructClaimStatus Status,
        IReadOnlyList<string> MatchedTokens
    );

    /// <summary>Every construct string mapped to the DTD element/attribute names its text names.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> MapConstructsToDtdTokens(
        IReadOnlyList<string> constructs,
        SemanticInventory inventory
    )
    {
        ArgumentNullException.ThrowIfNull(constructs);
        ArgumentNullException.ThrowIfNull(inventory);

        string[] elementNames = inventory
            .Surfaces.Where(s => s.Kind == "element")
            .Select(s => s.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        // "an internal capital" = at least one uppercase letter after the first character, which is
        // exactly the shape a multi-word camelCase attribute name has and a bare English word does not.
        string[] attributeNames = inventory
            .Surfaces.Where(s => s.Kind == "attribute")
            .Select(s => s.Name)
            .Distinct(StringComparer.Ordinal)
            .Where(name => name.Length > 1 && name[1..].Any(char.IsUpper))
            .ToArray();

        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (string construct in constructs)
        {
            var matched = new List<string>();
            foreach (string element in elementNames)
            {
                if (Regex.IsMatch(construct, $@"\b{Regex.Escape(element)}\b"))
                    matched.Add(element);
            }
            foreach (string attribute in attributeNames)
            {
                if (Regex.IsMatch(construct, $@"\b{Regex.Escape(attribute)}\b", RegexOptions.IgnoreCase))
                    matched.Add(attribute);
            }
            result[construct] = matched
                .Distinct(StringComparer.Ordinal)
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToArray();
        }
        return result;
    }

    /// <summary>Whether <paramref name="grammar"/> carries any element or non-empty attribute named in
    /// <paramref name="tokens"/>.</summary>
    private static bool GrammarContainsAnyToken(XDocument grammar, IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 0)
            return false;

        var tokenSet = new HashSet<string>(tokens, StringComparer.OrdinalIgnoreCase);
        foreach (XElement element in grammar.Descendants())
        {
            if (tokenSet.Contains(element.Name.LocalName))
                return true;
            foreach (XAttribute attribute in element.Attributes())
            {
                if (!string.IsNullOrEmpty(attribute.Value) && tokenSet.Contains(attribute.Name.LocalName))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// One row per <c>exercises:</c> claim across every fixture -- the same (fixture, word, signature,
    /// construct) shape <see cref="CoverageReport.WriteCsvs"/> already emits into
    /// <c>conformance/coverage.csv</c>, with a corroboration status joined on. Cheap: no reparse, just
    /// the DTD (once) and every fixture's already-loaded grammar.xml and words.yaml.
    /// </summary>
    public static IReadOnlyList<Row> Compute(string repositoryRoot, string constructsPath)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(constructsPath);

        SemanticInventory inventory = GrammarCoverageGate.ReadInventory(repositoryRoot);
        List<string> constructs = CoverageReport.LoadConstructChecklist(constructsPath);
        IReadOnlyDictionary<string, IReadOnlyList<string>> mapping = MapConstructsToDtdTokens(constructs, inventory);

        var rows = new List<Row>();
        foreach (Fixture fixture in Fixture.DiscoverAll(Path.Combine(repositoryRoot, "conformance")))
        {
            XDocument grammar = XDocument.Load(fixture.GrammarPath);
            var statusCache = new Dictionary<string, ConstructClaimStatus>(StringComparer.Ordinal);

            ConstructClaimStatus StatusFor(string construct)
            {
                if (statusCache.TryGetValue(construct, out ConstructClaimStatus cached))
                    return cached;

                IReadOnlyList<string> tokens = mapping.TryGetValue(construct, out IReadOnlyList<string>? found)
                    ? found
                    : Array.Empty<string>();
                ConstructClaimStatus status =
                    tokens.Count == 0 ? ConstructClaimStatus.Unmapped
                    : GrammarContainsAnyToken(grammar, tokens) ? ConstructClaimStatus.Confirmed
                    : ConstructClaimStatus.Contradicted;
                statusCache[construct] = status;
                return status;
            }

            void AddClaim(string word, string signature, string construct)
            {
                IReadOnlyList<string> tokens = mapping.TryGetValue(construct, out IReadOnlyList<string>? found)
                    ? found
                    : Array.Empty<string>();
                rows.Add(new Row(fixture.Id, word, signature, construct, StatusFor(construct), tokens));
            }

            foreach (WordEntry word in fixture.Words.Words)
            {
                if (word.ExpectFail || word.ExpectSkip)
                {
                    foreach (string construct in word.Exercises)
                        AddClaim(word.Word, "", construct);
                }
                else
                {
                    foreach (ParseEntry parse in word.Parses)
                    {
                        foreach (
                            string construct in parse.Exercises.Concat(word.Exercises).Distinct(StringComparer.Ordinal)
                        )
                        {
                            AddClaim(word.Word, parse.Signature, construct);
                        }
                    }
                }
            }
        }

        return rows.OrderBy(r => r.Fixture, StringComparer.Ordinal)
            .ThenBy(r => r.Word, StringComparer.Ordinal)
            .ThenBy(r => r.Signature, StringComparer.Ordinal)
            .ThenBy(r => r.Construct, StringComparer.Ordinal)
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
        writer.WriteLine("# GENERATED by hc-conformance --write-coverage-traceability. One row per words.yaml");
        writer.WriteLine("# 'exercises:' claim (self-reported by the fixture author), cross-checked against that");
        writer.WriteLine(
            "# fixture's OWN grammar.xml. This is a STRUCTURAL check, not a semantic one: Confirmed means"
        );
        writer.WriteLine(
            "# the construct text names a DTD element/attribute this grammar actually contains, never that"
        );
        writer.WriteLine(
            "# the word's parse depends on it. Contradicted means identifiers were found in the construct's"
        );
        writer.WriteLine(
            "# text but none appear in this grammar -- the claim looks wrong. Unmapped means the construct's"
        );
        writer.WriteLine(
            "# text contains no identifier ConstructClaimCorroboration recognizes, so it cannot be checked"
        );
        writer.WriteLine(
            "# either way. matched_tokens is empty for Unmapped. signature is \"-\" for expect_fail/expect_skip words."
        );
        writer.WriteLine("fixture\tword\tsignature\tconstruct\tstatus\tmatched_tokens");
        foreach (
            Row row in rows.OrderBy(r => r.Fixture, StringComparer.Ordinal)
                .ThenBy(r => r.Word, StringComparer.Ordinal)
                .ThenBy(r => r.Signature, StringComparer.Ordinal)
                .ThenBy(r => r.Construct, StringComparer.Ordinal)
        )
        {
            writer.WriteLine(
                string.Join(
                    '\t',
                    row.Fixture,
                    row.Word,
                    row.Signature.Length == 0 ? NullField : row.Signature,
                    row.Construct,
                    row.Status,
                    // Trailing-tab safety: Read() trims each raw line, which would otherwise eat an
                    // empty last field (Unmapped rows have no matched tokens) along with real
                    // whitespace -- see InterfaceInventoryLedger's identical fix for why "-" and not "".
                    row.MatchedTokens.Count == 0
                        ? NullField
                        : string.Join(",", row.MatchedTokens)
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
                || line.StartsWith("fixture\t", StringComparison.Ordinal)
            )
            {
                continue;
            }

            string[] fields = line.Split('\t');
            if (fields.Length != ColumnCount)
                throw new FormatException($"{RelativePath}: '{line}' must be {ColumnCount} tab-separated fields");
            if (!Enum.TryParse(fields[4], out ConstructClaimStatus status))
                throw new FormatException($"{RelativePath}: unknown status '{fields[4]}'");

            IReadOnlyList<string> tokens = fields[5] is "" or NullField ? Array.Empty<string>() : fields[5].Split(',');
            rows.Add(new Row(fields[0], fields[1], fields[2] == NullField ? "" : fields[2], fields[3], status, tokens));
        }

        return rows;
    }
}
