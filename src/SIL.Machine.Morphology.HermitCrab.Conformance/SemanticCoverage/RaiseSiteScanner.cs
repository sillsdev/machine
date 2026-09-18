#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Finds every source location in the core engine (<c>src/SIL.Machine.Morphology.HermitCrab</c>, never
/// this Conformance project or the Tool project, which only ever READ a <see cref="FailureReason"/>
/// back off a trace) that passes a non-<see cref="FailureReason.None"/> member as a value -- the
/// <c>raise_sites</c> column <see cref="EngineGateInventoryLedger"/> reports per gate.
///
/// Purely textual: a literal <c>FailureReason.Member</c> occurrence, skipping a `//`-commented line and
/// a `==`/`!=` comparison (the only two shapes that appear in this engine and are NOT a raise -- see
/// e.g. <c>ParseCommand.cs</c>'s and <c>SynthesisRewriteRule.cs</c>'s own comparisons against
/// <see cref="FailureReason.None"/>). Deliberately mechanical rather than a hand-curated list, so the
/// checked-in ledger cannot silently drift from the source it claims to describe.
/// </summary>
public static class RaiseSiteScanner
{
    private static readonly Regex Reference = new(@"FailureReason\.(\w+)", RegexOptions.Compiled);

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Scan(string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        string engineRoot = Path.Combine(repositoryRoot, "src", "SIL.Machine.Morphology.HermitCrab");
        var sites = new Dictionary<string, List<(string File, int Line)>>(StringComparer.Ordinal);

        foreach (string path in Directory.EnumerateFiles(engineRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (
                path.Contains(
                    $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal
                )
                || path.Contains(
                    $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal
                )
            )
            {
                continue;
            }

            string fileName = Path.GetFileName(path);
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                    continue;

                foreach (Match match in Reference.Matches(line))
                {
                    string member = match.Groups[1].Value;
                    if (member == "None")
                        continue;

                    string before = line[..match.Index].TrimEnd();
                    if (
                        before.EndsWith("==", StringComparison.Ordinal)
                        || before.EndsWith("!=", StringComparison.Ordinal)
                    )
                    {
                        continue;
                    }

                    if (!sites.TryGetValue(member, out List<(string File, int Line)>? list))
                        sites[member] = list = new List<(string, int)>();
                    list.Add((fileName, i + 1));
                }
            }
        }

        return sites.ToDictionary(
            kv => kv.Key,
            kv =>
                (IReadOnlyList<string>)
                    kv
                        .Value.OrderBy(s => s.File, StringComparer.Ordinal)
                        .ThenBy(s => s.Line)
                        .Select(s => $"{s.File}:{s.Line}")
                        .ToArray(),
            StringComparer.Ordinal
        );
    }
}
