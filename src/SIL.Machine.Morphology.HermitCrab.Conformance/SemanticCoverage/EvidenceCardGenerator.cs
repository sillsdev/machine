#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>One rendered card: <see cref="Markdown"/> is the entire file content, ready to write as-is
/// under <see cref="EvidenceCardGenerator.RelativeDirectory"/>/<see cref="FileName"/>.</summary>
public sealed record EvidenceCard(string CellId, string FileName, string Markdown);

/// <summary>The result of comparing freshly <see cref="EvidenceCardGenerator.Compute"/>d cards against
/// what is checked in under <see cref="EvidenceCardGenerator.RelativeDirectory"/>.</summary>
public sealed record EvidenceCardDiff(int StaleOrMissingCount, int ExtraFileCount, IReadOnlyList<string> Details)
{
    public bool IsCurrent => StaleOrMissingCount == 0 && ExtraFileCount == 0;
}

/// <summary>
/// Renders one reviewable Markdown card per row of <see cref="DataflowObligationLedger"/> -- the
/// per-cell join docs/dataflow-coverage-plan.md's "contract" section calls for, so a reviewer reads one
/// screen instead of hand-joining <c>dataflow-obligations.tsv</c>, <c>interface-witness.tsv</c>,
/// <c>words.yaml</c>'s <c>claimed_cells</c>/<c>note</c>, and <c>grammar.xml</c>.
///
/// <para>
/// <b>This is not a gate.</b> It reads four already-checked-in, already-authoritative sources and
/// renders their contents; it computes no new verdict and writes to none of them. The single fact it
/// decides for itself is which (fixture, word) a cell's evidence refers to, and that decision is a text
/// extraction from <see cref="DataflowObligationLedger.Row.Evidence"/> (a fixed shape written by
/// <see cref="DataflowObligationLedger"/> itself) or a direct read of a <c>claimed_cells</c> entry --
/// never a fresh severance run.
/// </para>
///
/// <para>
/// <b>Absence is rendered, never invented.</b> A cell with no <c>claimed_cells</c> entry, no
/// extractable witnessing word, no <c>proof:</c>, or no named <c>distinct_from</c> says so in the
/// card's own words rather than leaving the section blank -- see this class's own falsification test
/// for why (an empty section read as "nothing to see" is exactly how an unexamined claim gets waved
/// through).
/// </para>
/// </summary>
public static class EvidenceCardGenerator
{
    public const string RelativeDirectory = "conformance/evidence-cards";
    private const string IndexFileName = "index.tsv";

    private static readonly Regex PairedWitnessPattern = new(
        @"flip '(?<word>[^']+)' from failed to successful parse in (?<fixture>\S+) \(",
        RegexOptions.Compiled
    );

    // Matches the two other evidence shapes DataflowObligationLedger writes that name a fixture but no
    // word: "structurally hazardous: <fixture> declares..." (Mutator cells) and "...observed in
    // <fixture> ('...')" (ConditionExtension cells). Never invents a fixture the evidence text does not
    // itself name.
    private static readonly Regex FixtureOnlyPattern = new(
        @"(?:hazardous:\s+|observed in\s+)(?<fixture>(?:languages|edge-cases)/[A-Za-z0-9\-]+)",
        RegexOptions.Compiled
    );

    public static IReadOnlyList<EvidenceCard> Compute(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);

        IReadOnlyList<DataflowObligationLedger.Row> cells = DataflowObligationLedger.Read(repositoryRoot);
        IReadOnlyList<InterfaceWitnessResult> witnesses = InterfaceWitnessLedger.Read(repositoryRoot);
        IReadOnlyList<Fixture> fixtures = Fixture.DiscoverAll(Path.Combine(repositoryRoot, "conformance"));

        var witnessByKey = new Dictionary<(string Element, string Attribute, string FixtureId), InterfaceWitnessResult>();
        foreach (InterfaceWitnessResult w in witnesses)
            witnessByKey[(w.Element, w.Attribute, w.FixtureId)] = w;

        var claimsByCellId = new Dictionary<string, List<(string FixtureId, WordEntry Word, ClaimedCellEntry Claim)>>(
            StringComparer.Ordinal
        );
        foreach (Fixture fixture in fixtures)
        {
            foreach (WordEntry word in fixture.Words.Words)
            {
                foreach (ClaimedCellEntry claim in word.ClaimedCells)
                {
                    if (!claimsByCellId.TryGetValue(claim.Cell, out List<(string, WordEntry, ClaimedCellEntry)>? list))
                    {
                        list = new List<(string, WordEntry, ClaimedCellEntry)>();
                        claimsByCellId[claim.Cell] = list;
                    }
                    list.Add((fixture.Id, word, claim));
                }
            }
        }

        var cards = new List<EvidenceCard>();
        var seenFileNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (DataflowObligationLedger.Row row in cells)
        {
            IReadOnlyList<(string FixtureId, WordEntry Word, ClaimedCellEntry Claim)> claims = claimsByCellId.TryGetValue(
                row.CellId,
                out List<(string, WordEntry, ClaimedCellEntry)>? found
            )
                ? found
                : Array.Empty<(string, WordEntry, ClaimedCellEntry)>();

            (string? FixtureId, string? Word, string Source) primary = ResolvePrimaryWitness(row, claims);
            string markdown = BuildMarkdown(row, claims, primary, witnessByKey, fixtures, repositoryRoot);
            string fileName = SlugFileName(row.CellId);
            if (!seenFileNames.Add(fileName))
            {
                throw new InvalidOperationException(
                    $"evidence card filename collision for cell '{row.CellId}' -> '{fileName}'"
                );
            }

            cards.Add(new EvidenceCard(row.CellId, fileName, markdown));
        }

        return cards;
    }

    /// <summary>Writes every card plus <c>index.tsv</c>, and removes any file in the directory that is
    /// not part of the fresh set -- so a deleted or renamed cell's stale card can never linger unnoticed
    /// next to a Check() that only compares what it expects to find.</summary>
    public static void Write(string repositoryRoot, IReadOnlyList<EvidenceCard> cards)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(cards);

        string dir = Path.Combine(repositoryRoot, RelativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(dir);

        var expected = new HashSet<string>(cards.Select(c => c.FileName), StringComparer.Ordinal) { IndexFileName };
        foreach (string existing in Directory.EnumerateFiles(dir))
        {
            if (!expected.Contains(Path.GetFileName(existing)))
                File.Delete(existing);
        }

        foreach (EvidenceCard card in cards)
            File.WriteAllText(Path.Combine(dir, card.FileName), card.Markdown);

        File.WriteAllText(Path.Combine(dir, IndexFileName), ToIndexText(cards));
    }

    /// <summary>Compares freshly computed cards against the checked-in directory without writing
    /// anything -- the drift check a build runs.</summary>
    public static EvidenceCardDiff Check(string repositoryRoot, IReadOnlyList<EvidenceCard> cards)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(cards);

        string dir = Path.Combine(repositoryRoot, RelativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        var details = new List<string>();
        int staleOrMissing = 0;

        foreach (EvidenceCard card in cards)
        {
            string path = Path.Combine(dir, card.FileName);
            if (!File.Exists(path))
            {
                staleOrMissing++;
                details.Add($"MISSING {card.CellId} ({card.FileName})");
                continue;
            }

            string onDisk = File.ReadAllText(path).ReplaceLineEndings("\n");
            string fresh = card.Markdown.ReplaceLineEndings("\n");
            if (!string.Equals(onDisk, fresh, StringComparison.Ordinal))
            {
                staleOrMissing++;
                details.Add($"STALE {card.CellId} ({card.FileName})");
            }
        }

        string indexPath = Path.Combine(dir, IndexFileName);
        string freshIndex = ToIndexText(cards).ReplaceLineEndings("\n");
        if (!File.Exists(indexPath) || !string.Equals(File.ReadAllText(indexPath).ReplaceLineEndings("\n"), freshIndex, StringComparison.Ordinal))
        {
            staleOrMissing++;
            details.Add($"STALE {IndexFileName}");
        }

        var expected = new HashSet<string>(cards.Select(c => c.FileName), StringComparer.Ordinal) { IndexFileName };
        int extra = 0;
        if (Directory.Exists(dir))
        {
            foreach (string existing in Directory.EnumerateFiles(dir))
            {
                string name = Path.GetFileName(existing);
                if (expected.Contains(name))
                    continue;
                extra++;
                details.Add($"EXTRA FILE {name} (not produced by any current cell -- regenerate with --write-evidence-cards)");
            }
        }

        return new EvidenceCardDiff(staleOrMissing, extra, details);
    }

    public static string ToIndexText(IReadOnlyList<EvidenceCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);
        var writer = new StringWriter();
        writer.WriteLine(
            "# GENERATED by hc-conformance --write-evidence-cards. Maps each conformance/dataflow-obligations.tsv"
        );
        writer.WriteLine(
            "# cell_id to the Markdown card that renders it under conformance/evidence-cards/ -- file names are"
        );
        writer.WriteLine(
            "# a truncated, sanitized slug of the cell id plus a stable content hash (cell ids contain ':'/'.'"
        );
        writer.WriteLine("# /'->' and can exceed Windows path-length limits verbatim), so this index is the lookup a");
        writer.WriteLine("# human or gate uses instead of reconstructing the file name by hand.");
        writer.WriteLine("cell_id\tfile_name");
        foreach (EvidenceCard card in cards.OrderBy(c => c.CellId, StringComparer.Ordinal))
            writer.WriteLine($"{card.CellId}\t{card.FileName}");
        return writer.ToString();
    }

    // ----------------------------------------------------------------------------------------
    // Resolving which (fixture, word), if any, this cell's evidence refers to.
    // ----------------------------------------------------------------------------------------

    private static (string? FixtureId, string? Word, string Source) ResolvePrimaryWitness(
        DataflowObligationLedger.Row row,
        IReadOnlyList<(string FixtureId, WordEntry Word, ClaimedCellEntry Claim)> claims
    )
    {
        if (claims.Count > 0)
            return (claims[0].FixtureId, claims[0].Word.Word, "claimed_cells entry in words.yaml");

        Match paired = PairedWitnessPattern.Match(row.Evidence);
        if (paired.Success)
        {
            return (
                paired.Groups["fixture"].Value,
                paired.Groups["word"].Value,
                "extracted from this cell's dataflow-obligations.tsv evidence text"
            );
        }

        Match fixtureOnly = FixtureOnlyPattern.Match(row.Evidence);
        if (fixtureOnly.Success)
        {
            return (
                fixtureOnly.Groups["fixture"].Value,
                null,
                "extracted from this cell's dataflow-obligations.tsv evidence text (names a fixture, no specific word)"
            );
        }

        return (null, null, "none -- no claim and no fixture/word named in this cell's evidence text");
    }

    // ----------------------------------------------------------------------------------------
    // Plain-English role text.
    // ----------------------------------------------------------------------------------------

    private static readonly Regex ConditionExtensionRolePattern = new(
        @"^McDcVector(?<n>\d+)(?<half>Control|GatedForm)$",
        RegexOptions.Compiled
    );

    private static string MutatorClassPlainEnglish(string mutatorClass) =>
        mutatorClass switch
        {
            "Overwrite" =>
                "an MPR-feature overwrite group (a MorphologicalPhonologicalRuleFeatureGroup with "
                    + "outputType=\"overwrite\" drops the accumulated feature set before the read)",
            "Blocking" =>
                "blocking (Word.CheckBlocking rebuilds the derivation from a sibling LexicalEntry, clearing "
                    + "and reseeding MPR features, part of speech, and stem name)",
            "PosPriorityUnion" =>
                "a part-of-speech priority-union clobber (an intervening rule's own outputPartOfSpeech "
                    + "overwrites an earlier part-of-speech write via PriorityUnion)",
            "CompoundingNonHeadDrop" =>
                "a compounding non-head drop (the output word is built from the head alone, so the "
                    + "non-head's payload is never copied into it)",
            _ => mutatorClass,
        };

    private static string RolePlainEnglish(DataflowObligationLedger.Row row)
    {
        switch (row.CellKind)
        {
            case "McDc":
                return row.Role switch
                {
                    "PresentControl" =>
                        "the payload IS present, and the reader does NOT block (control case: normal "
                            + "operation with the payload present).",
                    "PresentGatedForm" => "the payload IS present and the gated form IS blocked.",
                    "AbsentControl" =>
                        "the payload is ABSENT, and the reader does NOT block (control case: normal "
                            + "operation without the payload).",
                    "AbsentGatedForm" => "the payload is ABSENT and the gated form IS blocked.",
                    _ => $"(unrecognized McDc role '{row.Role}' -- this generator does not know how to describe it)",
                };

            case "ConditionExtension":
            {
                Match m = ConditionExtensionRolePattern.Match(row.Role);
                if (!m.Success)
                    return $"(unrecognized ConditionExtension role '{row.Role}' -- this generator does not know how to describe it)";

                string half = m.Groups["half"].Value == "GatedForm"
                    ? "the gated form IS blocked"
                    : "the reader does NOT block (control)";
                return
                    $"Extra MC/DC vector #{m.Groups["n"].Value}, required because the reader's gate has more than "
                        + $"one condition -- see the chain kind below. This arm is where {half}.";
            }

            case "Mutator":
                return row.Role switch
                {
                    "MutatorAbsent" =>
                        $"no mutator sits between the write and the read: {MutatorClassPlainEnglish(row.MutatorClass)} "
                            + "is ABSENT from the path.",
                    "MutatorPresent" =>
                        $"a mutator sits between the write and the read: {MutatorClassPlainEnglish(row.MutatorClass)} "
                            + "is PRESENT and structurally capable of killing the payload before the reader sees it.",
                    _ => $"(unrecognized Mutator role '{row.Role}' -- this generator does not know how to describe it)",
                };

            default:
                return $"(unrecognized cell_kind '{row.CellKind}' -- this generator does not know how to describe it)";
        }
    }

    // ----------------------------------------------------------------------------------------
    // Markdown rendering.
    // ----------------------------------------------------------------------------------------

    private static string BuildMarkdown(
        DataflowObligationLedger.Row row,
        IReadOnlyList<(string FixtureId, WordEntry Word, ClaimedCellEntry Claim)> claims,
        (string? FixtureId, string? Word, string Source) primary,
        IReadOnlyDictionary<(string Element, string Attribute, string FixtureId), InterfaceWitnessResult> witnessByKey,
        IReadOnlyList<Fixture> fixtures,
        string repositoryRoot
    )
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {row.CellId}");
        sb.AppendLine();
        sb.AppendLine(
            "GENERATED by `hc-conformance --write-evidence-cards`. Do not hand-edit; regenerate instead -- see "
                + "`docs/coverage-review-protocol.md`."
        );
        sb.AppendLine();

        sb.AppendLine("## Role, in plain English");
        sb.AppendLine();
        sb.AppendLine(RolePlainEnglish(row));
        sb.AppendLine();

        sb.AppendLine("## Chain");
        sb.AppendLine();
        sb.AppendLine($"- Writer: `{row.WriterElement}.{row.WriterAttribute}`");
        sb.AppendLine($"- Payload type: `{row.PayloadType}`");
        sb.AppendLine($"- Reader: `{row.ReaderElement}.{row.ReaderAttribute}`");
        sb.AppendLine(
            row.MutatorClass == "-"
                ? $"- Cell kind: `{row.CellKind}`"
                : $"- Cell kind: `{row.CellKind}`, mutator class: `{row.MutatorClass}`"
        );
        sb.AppendLine();

        sb.AppendLine("## Machine status");
        sb.AppendLine();
        sb.AppendLine(
            $"**{row.Status}** -- machine-established (`conformance/dataflow-obligations.tsv`), never a review "
                + "verdict. A human sign-off is a separate fact and is never recorded here."
        );
        sb.AppendLine();
        sb.AppendLine($"Ledger evidence: {row.Evidence}");
        sb.AppendLine();

        sb.AppendLine("## Fixture and word");
        sb.AppendLine();
        if (claims.Count > 0)
        {
            foreach ((string fixtureId, WordEntry word, ClaimedCellEntry _) in claims)
                sb.AppendLine($"- Claimed by word **'{word.Word}'** in `{fixtureId}` (a `claimed_cells` entry in `words.yaml`).");
        }
        else if (primary.FixtureId is not null)
        {
            sb.AppendLine(
                primary.Word is not null
                    ? $"- No `claimed_cells` entry names this cell. The ledger's own evidence names word "
                        + $"**'{primary.Word}'** in `{primary.FixtureId}` ({primary.Source})."
                    : $"- No `claimed_cells` entry names this cell, and the ledger's evidence names no specific "
                        + $"word -- only fixture `{primary.FixtureId}` ({primary.Source})."
            );
        }
        else
        {
            sb.AppendLine("- No fixture or word is identified for this cell: no claim, and the ledger's evidence names none.");
        }
        sb.AppendLine();

        string[] fixtureIdsInScope = claims.Count > 0
            ? claims.Select(c => c.FixtureId).Distinct(StringComparer.Ordinal).ToArray()
            : primary.FixtureId is not null
                ? new[] { primary.FixtureId }
                : Array.Empty<string>();

        sb.AppendLine("## Exact mutation and before/after parse");
        sb.AppendLine();
        if (claims.Count == 0)
        {
            sb.AppendLine("No `claimed_cells` entry recorded an author-reviewed severing/before/after for this cell.");
            sb.AppendLine();
        }
        else
        {
            foreach ((string fixtureId, WordEntry word, ClaimedCellEntry claim) in claims)
            {
                sb.AppendLine($"### Author's claim ({fixtureId} / '{word.Word}')");
                sb.AppendLine();
                if (claim.HasReviewBundle)
                {
                    sb.AppendLine($"- Severing: {claim.Severing}");
                    sb.AppendLine($"- Before: `{claim.Before}`");
                    sb.AppendLine($"- After: `{claim.After}`");
                }
                else
                {
                    sb.AppendLine("- No severing/before/after recorded on this claim yet (unreviewed).");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("### Machine witness (`conformance/interface-witness.tsv`)");
        sb.AppendLine();
        if (fixtureIdsInScope.Length == 0)
        {
            sb.AppendLine("No fixture is identified for this cell, so no witness row can be looked up.");
        }
        else
        {
            foreach (string fixtureId in fixtureIdsInScope)
            {
                AppendWitnessLine(sb, witnessByKey, row.WriterElement, row.WriterAttribute, fixtureId, "Writer");
                AppendWitnessLine(sb, witnessByKey, row.ReaderElement, row.ReaderAttribute, fixtureId, "Reader");
            }
        }
        sb.AppendLine();

        sb.AppendLine("## Grammar citations");
        sb.AppendLine();
        if (fixtureIdsInScope.Length == 0)
        {
            sb.AppendLine("No fixture is identified for this cell, so no `grammar.xml` lines can be cited.");
        }
        else
        {
            foreach (string fixtureId in fixtureIdsInScope)
            {
                string grammarPath = Path.Combine(
                    repositoryRoot,
                    "conformance",
                    fixtureId.Replace('/', Path.DirectorySeparatorChar),
                    "grammar.xml"
                );
                sb.AppendLine($"### `{fixtureId}/grammar.xml`");
                sb.AppendLine();
                AppendCitations(sb, grammarPath, row.WriterElement, row.WriterAttribute, "Writer (payload declared here)");
                AppendCitations(sb, grammarPath, row.ReaderElement, row.ReaderAttribute, "Reader (gate declared here)");
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Author's prose");
        sb.AppendLine();
        if (claims.Count > 0)
        {
            foreach ((string fixtureId, WordEntry word, ClaimedCellEntry claim) in claims)
            {
                if (claim.Proof.Length != 0)
                {
                    sb.AppendLine($"- `proof:` (claimed_cells, '{word.Word}' in {fixtureId}): {claim.Proof}");
                }
                else if (word.Note.Length != 0)
                {
                    sb.AppendLine(
                        $"- No `proof:` on this claim. Falling back to the word's `note:` ('{word.Word}' in "
                            + $"{fixtureId}): {word.Note}"
                    );
                }
                else
                {
                    sb.AppendLine($"- No prose recorded: '{word.Word}' in {fixtureId} has neither a claim `proof:` nor a word `note:`.");
                }
            }
        }
        else if (primary is { FixtureId: not null, Word: not null })
        {
            WordEntry? witnessWord = fixtures
                .FirstOrDefault(f => f.Id == primary.FixtureId)
                ?.Words.Words.FirstOrDefault(w => w.Word == primary.Word);
            if (witnessWord is { Note.Length: > 0 })
            {
                sb.AppendLine(
                    $"- No claim exists for this cell. This is the witnessing word's `note:` ('{primary.Word}' in "
                        + $"{primary.FixtureId}): {witnessWord.Note}"
                );
            }
            else
            {
                sb.AppendLine("- No prose recorded: no claim exists, and the witnessing word has no `note:`.");
            }
        }
        else
        {
            sb.AppendLine("- No prose recorded: no claim, and no word is identified for this cell.");
        }
        sb.AppendLine();

        sb.AppendLine("## `distinct_from` counterpart");
        sb.AppendLine();
        if (claims.Count == 0)
        {
            sb.AppendLine("No claim exists for this cell, so no `distinct_from` counterpart is recorded.");
        }
        else
        {
            foreach ((string fixtureId, WordEntry word, ClaimedCellEntry claim) in claims)
            {
                if (claim.DistinctFrom.Length == 0)
                {
                    sb.AppendLine($"- '{word.Word}' in {fixtureId}: no `distinct_from` named.");
                    continue;
                }

                Fixture? fixture = fixtures.FirstOrDefault(f => f.Id == fixtureId);
                WordEntry? counterpart = fixture?.Words.Words.FirstOrDefault(w => w.Word == claim.DistinctFrom);
                sb.AppendLine(
                    counterpart is null
                        ? $"- '{word.Word}' in {fixtureId}: `distinct_from` names **'{claim.DistinctFrom}'**, which "
                            + "does not exist in this fixture."
                        : $"- '{word.Word}' in {fixtureId}: `distinct_from` **'{counterpart.Word}'** "
                            + $"(expect_fail={counterpart.ExpectFail}) vs. this word (expect_fail={word.ExpectFail})."
                );
            }
        }

        return sb.ToString();
    }

    private static void AppendWitnessLine(
        StringBuilder sb,
        IReadOnlyDictionary<(string Element, string Attribute, string FixtureId), InterfaceWitnessResult> witnessByKey,
        string element,
        string attribute,
        string fixtureId,
        string role
    )
    {
        if (witnessByKey.TryGetValue((element, attribute, fixtureId), out InterfaceWitnessResult? w))
        {
            string example = w.ExampleWord is null
                ? ""
                : $", example: '{w.ExampleWord}': {w.ExampleOutcome} -> {w.CounterexampleOutcome}";
            sb.AppendLine(
                $"- {role} (`{element}.{attribute}` in `{fixtureId}`): verdict={w.Verdict}, mutation=\"{w.Mutation}\"{example}"
            );
        }
        else
        {
            sb.AppendLine(
                $"- {role} (`{element}.{attribute}` in `{fixtureId}`): no row in `interface-witness.tsv` for this "
                    + "(element, attribute, fixture) triple."
            );
        }
    }

    private static void AppendCitations(StringBuilder sb, string grammarPath, string element, string attribute, string role)
    {
        if (!File.Exists(grammarPath))
        {
            sb.AppendLine($"- {role} `{element}.{attribute}`: `grammar.xml` not found at `{grammarPath}`.");
            return;
        }

        XDocument doc = XDocument.Load(grammarPath, LoadOptions.SetLineInfo);
        (int Line, string Value)[] occurrences = doc.Descendants(element)
            .Select(el => (Element: el, Attr: el.Attribute(attribute)))
            .Where(x => x.Attr is not null)
            .Select(x => (Line: ((IXmlLineInfo)x.Element).LineNumber, Value: x.Attr!.Value))
            .OrderBy(x => x.Line)
            .ToArray();

        if (occurrences.Length == 0)
        {
            sb.AppendLine($"- {role} `{element}.{attribute}`: no occurrence found in this fixture's `grammar.xml`.");
            return;
        }

        foreach ((int line, string value) in occurrences)
            sb.AppendLine($"- {role} `{element}.{attribute}`: `grammar.xml:{line}` = \"{value}\"");
    }

    // ----------------------------------------------------------------------------------------
    // Filenames: cell ids contain "::", "->", ":", and "." -- ":" and the ">" half of "->" are
    // illegal or reserved in a Windows path, and a cell id can run past 170 characters, which risks
    // MAX_PATH once joined to a repository path. Sanitizing to [A-Za-z0-9._-] and truncating keeps the
    // name short and legal; the appended hash (stable across processes -- never string.GetHashCode,
    // which .NET randomizes per process) is what actually guarantees uniqueness, so truncation can
    // never silently collapse two distinct cells onto one file.
    // ----------------------------------------------------------------------------------------

    private const int MaxSlugLength = 100;

    private static string SlugFileName(string cellId)
    {
        string safe = new(cellId.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_').ToArray());
        safe = Regex.Replace(safe, "_+", "_").Trim('_');
        if (safe.Length > MaxSlugLength)
            safe = safe.Substring(0, MaxSlugLength);

        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cellId))).Substring(0, 10).ToLowerInvariant();
        return $"{safe}__{hash}.md";
    }
}
