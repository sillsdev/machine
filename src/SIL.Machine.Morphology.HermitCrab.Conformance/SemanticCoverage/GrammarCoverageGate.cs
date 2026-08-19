#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

public sealed record GrammarCoverageResult(
    IReadOnlyList<string> Observable,
    IReadOnlyList<string> Covered,
    IReadOnlyList<string> Uncovered,
    IReadOnlyDictionary<string, IReadOnlyList<string>> FixturesBySurface
);

/// <summary>
/// Recomputes which grammar-observable surfaces the checked-in fixtures exercise and compares the
/// uncovered set to a baseline file. Coverage is read from each grammar document rather than
/// declared by hand, so a fixture cannot claim a surface it does not contain.
/// </summary>
public static class GrammarCoverageGate
{
    public const string BaselineRelativePath = "conformance/semantic-coverage-baseline.txt";

    /// <summary>The published copy of the grammar DTD, under <c>conformance/</c>.</summary>
    /// <remarks>A consumer can receive <c>conformance/</c> alone, sparse-checked-out with no other
    /// part of this repository present, so the authority a published product names has to live inside
    /// it. The library's
    /// own copy stays where it is because it is an embedded resource
    /// (<c>SIL.Machine.Morphology.HermitCrab.HermitCrabInput.dtd</c>) that
    /// <c>XmlLanguageLoader</c> reads by manifest name; the two are held byte-identical by
    /// <c>PublishedDtdMatchesTheLibraryResource</c>.</remarks>
    public const string DtdRelativePath = "conformance/HermitCrabInput.dtd";

    /// <summary>The library's embedded copy, authoritative for the shipped assembly.</summary>
    public const string LibraryDtdRelativePath = "src/SIL.Machine.Morphology.HermitCrab/HermitCrabInput.dtd";

    public static SemanticInventory ReadInventory(string repositoryRoot) =>
        SemanticCoverageInventory.Generate(
            SemanticCoverageSourceSet.FromDtd(
                "HermitCrabInput.dtd",
                File.ReadAllText(
                    Path.Combine(repositoryRoot, DtdRelativePath.Replace('/', Path.DirectorySeparatorChar))
                )
            )
        );

    /// <summary>Every fixture grammar under <c>conformance/</c>, keyed by its directory name.</summary>
    public static IReadOnlyList<(string FixtureId, string GrammarPath)> DiscoverGrammars(string repositoryRoot)
    {
        string conformance = Path.Combine(repositoryRoot, "conformance");
        if (!Directory.Exists(conformance))
            return Array.Empty<(string, string)>();
        return Directory
            .GetFiles(conformance, "grammar.xml", SearchOption.AllDirectories)
            .Select(path => (FixtureId: FixtureIdFor(path), GrammarPath: path))
            .OrderBy(item => item.FixtureId, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Grades every covered surface by the strongest evidence any fixture offers for it, using each
    /// fixture's verified fired-rule lists. Trace strength means a parse was attributed to the rule
    /// that owns the construct; presence means the construct was declared and never exercised.
    /// </summary>
    public static IReadOnlyList<SurfaceEvidence> GradeEvidence(string repositoryRoot, SemanticInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var best = new Dictionary<string, SurfaceEvidence>(StringComparer.Ordinal);
        foreach (Fixture fixture in Fixture.DiscoverAll(Path.Combine(repositoryRoot, "conformance")))
        {
            var fired = new HashSet<string>(StringComparer.Ordinal);
            bool hasVerifiedParse = false;
            var neutralized = new HashSet<string>(StringComparer.Ordinal);

            // A fixture CI never validates cannot supply trace data. budget_ms fixtures are excluded
            // from every default run, and an expect_crash fixture passes on any exception with no word
            // results at all, so neither one's rules: lists are ever compared against a real trace.
            // They are still GRADED, at presence: skipping them outright let their surfaces escape the
            // presence gate instead of failing it.
            bool validated = fixture.Words.BudgetMs is null && !fixture.Words.ExpectCrash;

            foreach (
                WordEntry word in validated ? (IEnumerable<WordEntry>)fixture.Words.Words : Array.Empty<WordEntry>()
            )
            {
                foreach (string declarationId in word.Neutralizes)
                    neutralized.Add(declarationId);

                // blocked_by is deliberately NOT folded in. Runner verifies each parse's rules list
                // against the trace by set equality, but an expect_fail word returns before that
                // check, so blocked_by is an author's assertion no engine run confirms.
                foreach (ParseEntry parse in word.Parses)
                {
                    hasVerifiedParse = true;
                    foreach (string ruleId in parse.Rules)
                        fired.Add(ruleId);
                }
            }

            foreach (
                SurfaceEvidence evidence in TraceEvidence.Grade(
                    fixture.Id,
                    XDocument.Load(fixture.GrammarPath),
                    inventory,
                    fired,
                    hasVerifiedParse,
                    neutralized
                )
            )
            {
                if (
                    best.TryGetValue(evidence.SurfaceId, out SurfaceEvidence? existing)
                    && existing.Strength >= evidence.Strength
                )
                {
                    continue;
                }
                best[evidence.SurfaceId] = evidence;
            }
        }

        return best.Values.OrderBy(item => item.SurfaceId, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Matches Fixture.Id (category/name), so the two discovery paths cannot collide.</summary>
    private static string FixtureIdFor(string grammarPath)
    {
        string? dir = Path.GetDirectoryName(grammarPath);
        string name = Path.GetFileName(dir) ?? grammarPath;
        string? category = Path.GetFileName(Path.GetDirectoryName(dir));
        return string.IsNullOrEmpty(category) ? name : $"{category}/{name}";
    }

    public static GrammarCoverageResult Compute(string repositoryRoot, SemanticInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var observable = GrammarFeatureUsage.Observable(inventory).ToHashSet(StringComparer.Ordinal);
        var fixturesBySurface = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        foreach ((string fixtureId, string grammarPath) in DiscoverGrammars(repositoryRoot))
        {
            foreach (string surfaceId in GrammarFeatureUsage.Read(XDocument.Load(grammarPath), inventory))
            {
                if (!fixturesBySurface.TryGetValue(surfaceId, out List<string>? fixtures))
                {
                    fixtures = new List<string>();
                    fixturesBySurface[surfaceId] = fixtures;
                }

                if (!fixtures.Contains(fixtureId, StringComparer.Ordinal))
                    fixtures.Add(fixtureId);
            }
        }

        string[] covered = fixturesBySurface
            .Keys.Where(observable.Contains)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        string[] uncovered = observable
            .Except(covered, StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        return new GrammarCoverageResult(
            observable.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            covered,
            uncovered,
            fixturesBySurface.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal
            )
        );
    }

    /// <summary>Schema the implementation never consumes; no grammar can cover it.</summary>
    public const string DeadSchema = "dead-schema";

    /// <summary>Coverable by a fixture that nobody has written yet.</summary>
    public const string Todo = "todo";

    /// <summary>
    /// One value of an enumerated alphabet whose mechanism a sibling value already covers, such as
    /// the Greek letter naming a phonological variable. Declaring the remaining letters in a grammar
    /// exercises no further behaviour, so they are quotiented rather than padded for. Valid only
    /// while a sibling IS covered, which the gate checks.
    /// </summary>
    public const string AlphabetQuotient = "alphabet-quotient";

    /// <summary>
    /// An enumerated value identical to its attribute's DTD default. The loader reads the grammar with
    /// ValidationType.DTD, so the parser supplies that value for EVERY document; a fixture that writes
    /// it and one that omits it are indistinguishable, and no word can discriminate them. Recomputed
    /// from the inventory's own attribute-default surface, never trusted from the ledger.
    /// </summary>
    public const string DtdDefault = "dtd-default";

    public sealed record LedgerEntry(string SurfaceId, string Classification);

    /// <summary>Whether an enumerated surface's value is that attribute's declared DTD default.</summary>
    public static bool IsDtdDefault(string surfaceId, IReadOnlySet<string> declaredSurfaces)
    {
        ArgumentNullException.ThrowIfNull(surfaceId);
        ArgumentNullException.ThrowIfNull(declaredSurfaces);
        if (!surfaceId.StartsWith(GrammarFeatureUsage.EnumPrefix, StringComparison.Ordinal))
            return false;
        string rest = surfaceId[GrammarFeatureUsage.EnumPrefix.Length..];
        string[] parts = rest.Split('/');
        if (parts.Length != 3)
            return false;
        return declaredSurfaces.Contains($"dtd:attribute-default/{parts[0]}/{parts[1]}/default/{parts[2]}");
    }

    /// <summary>The <c>dtd:enum/Element/attribute</c> prefix an enumerated value belongs to.</summary>
    public static string? EnumeratedAttribute(string surfaceId)
    {
        ArgumentNullException.ThrowIfNull(surfaceId);
        if (!surfaceId.StartsWith(GrammarFeatureUsage.EnumPrefix, StringComparison.Ordinal))
            return null;
        int lastSlash = surfaceId.LastIndexOf('/');
        return lastSlash <= GrammarFeatureUsage.EnumPrefix.Length ? null : surfaceId[..lastSlash];
    }

    /// <summary>
    /// Quotient lines whose attribute has no covered sibling. Such a line claims a mechanism is
    /// exercised elsewhere when nothing exercises it, so it is a real gap wearing a waiver.
    /// </summary>
    public static IReadOnlyList<string> UnbackedQuotients(
        IReadOnlyList<LedgerEntry> ledger,
        IReadOnlyList<string> covered
    )
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(covered);
        var coveredAttributes = covered
            .Select(EnumeratedAttribute)
            .Where(prefix => prefix is not null)
            .ToHashSet(StringComparer.Ordinal)!;
        return ledger
            .Where(entry => entry.Classification == AlphabetQuotient)
            .Where(entry =>
            {
                string? attribute = EnumeratedAttribute(entry.SurfaceId);
                return attribute is null || !coveredAttributes.Contains(attribute);
            })
            .Select(entry => entry.SurfaceId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    public const string PresenceWaiverRelativePath = "conformance/semantic-coverage-presence-waivers.txt";

    /// <summary>
    /// Surfaces knowingly counted as covered on presence alone. Read from a file so the CLI and the
    /// test suite gate on the same list rather than each carrying its own copy.
    /// </summary>
    public static IReadOnlyList<string> ReadPresenceWaivers(string repositoryRoot) =>
        ReadListFile(
            Path.Combine(repositoryRoot, PresenceWaiverRelativePath.Replace('/', Path.DirectorySeparatorChar))
        );

    private static IReadOnlyList<string> ReadListFile(string path) =>
        File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length != 0 && !line.StartsWith("#", StringComparison.Ordinal))
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyList<LedgerEntry> ReadBaseline(string repositoryRoot)
    {
        string path = Path.Combine(repositoryRoot, BaselineRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var entries = new List<LedgerEntry>();
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;
            string[] fields = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 2)
            {
                throw new FormatException(
                    $"{BaselineRelativePath}: '{line}' must be '<surfaceId><TAB><classification>'"
                );
            }
            string classification = fields[1].Trim();
            if (classification is not (DeadSchema or Todo or AlphabetQuotient or DtdDefault))
            {
                throw new FormatException(
                    $"{BaselineRelativePath}: unknown classification '{classification}' for '{fields[0]}'"
                );
            }
            entries.Add(new LedgerEntry(fields[0].Trim(), classification));
        }

        return entries.OrderBy(entry => entry.SurfaceId, StringComparer.Ordinal).ToArray();
    }

    /// <summary>
    /// Classifies each uncovered surface by whether the engine references its owning element, so the
    /// justification is recomputed rather than trusted.
    /// </summary>
    /// <summary>
    /// Reclassifies the uncovered set. Dead schema is always recomputed from the engine source; an
    /// existing alphabet-quotient decision is human judgement and is carried forward, still subject
    /// to <see cref="UnbackedQuotients"/>.
    /// </summary>
    public static IReadOnlyList<LedgerEntry> Classify(
        string repositoryRoot,
        IReadOnlyList<string> uncovered,
        IReadOnlyList<LedgerEntry> existing
    )
    {
        ArgumentNullException.ThrowIfNull(existing);
        var quotiented = existing
            .Where(entry => entry.Classification == AlphabetQuotient)
            .Select(entry => entry.SurfaceId)
            .ToHashSet(StringComparer.Ordinal);
        return Classify(repositoryRoot, uncovered)
            .Select(entry =>
                entry.Classification == Todo && quotiented.Contains(entry.SurfaceId)
                    ? entry with
                    {
                        Classification = AlphabetQuotient,
                    }
                    : entry
            )
            .ToArray();
    }

    public static IReadOnlyList<LedgerEntry> Classify(string repositoryRoot, IReadOnlyList<string> uncovered)
    {
        var elements = uncovered
            .Select(DeadSchemaDetector.OwningElement)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToArray();
        IReadOnlySet<string> unreferenced = DeadSchemaDetector.FindUnreferenced(repositoryRoot, elements);
        IReadOnlySet<string> declared = ReadInventory(repositoryRoot)
            .Surfaces.Select(surface => surface.Id)
            .ToHashSet(StringComparer.Ordinal);
        return uncovered
            .OrderBy(id => id, StringComparer.Ordinal)
            .Select(id =>
            {
                string? owner = DeadSchemaDetector.OwningElement(id);
                if (owner is not null && unreferenced.Contains(owner))
                    return new LedgerEntry(id, DeadSchema);
                return new LedgerEntry(id, IsDtdDefault(id, declared) ? DtdDefault : Todo);
            })
            .ToArray();
    }

    /// <summary>Rewrites the ledger in place, preserving its comment header.</summary>
    public static void WriteBaseline(string repositoryRoot, IReadOnlyList<LedgerEntry> entries)
    {
        string path = Path.Combine(repositoryRoot, BaselineRelativePath.Replace('/', Path.DirectorySeparatorChar));
        string[] header = File.ReadAllLines(path)
            .TakeWhile(line => line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            .ToArray();
        var text = new StringBuilder();
        foreach (string line in header)
            text.Append(line).Append('\n');
        foreach (LedgerEntry entry in entries.OrderBy(item => item.SurfaceId, StringComparer.Ordinal))
            text.Append(entry.SurfaceId).Append('\t').Append(entry.Classification).Append('\n');
        File.WriteAllText(path, text.ToString());
    }
}
