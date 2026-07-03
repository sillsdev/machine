using System.Diagnostics;
using System.IO;
using System.Linq;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Formats the InstrumentedRule tree (see Morpher.AccumulateRuleStats) as a flat, grep-able text report:
/// one line per rule with its totals, followed by its bucket breakdowns sorted so the rarest (most
/// suspicious) buckets are easy to spot against the common case -- that's the "300 times vs 4 times, are
/// the 4 wrong?" comparison this whole feature exists for.
/// </summary>
internal static class RuleStatsReport
{
    public static void Write(TextWriter writer, string label, InstrumentedRule<Word, int> root)
    {
        writer.WriteLine($"==== {label} ====");
        if (root == null)
        {
            writer.WriteLine("(no rule tree)");
            return;
        }
        WriteRule(writer, root, "");
        writer.WriteLine();
    }

    private static void WriteRule(TextWriter writer, InstrumentedRule<Word, int> rule, string path)
    {
        if (rule == null)
            return;

        string fullPath = string.IsNullOrEmpty(path) ? rule.Name ?? "?" : $"{path} > {rule.Name}";

        if (rule.InputCount > 0 || rule.BucketGroups.Count > 0)
        {
            double elapsedMs = rule.ElapsedTime * 1000.0 / Stopwatch.Frequency;
            writer.WriteLine(
                $"{fullPath}\tinputs={rule.InputCount}\tsuccesses={rule.SuccessCount}\toutputs={rule.OutputCount}\telapsedMs={elapsedMs:F0}"
            );

            foreach (var group in rule.BucketGroups.OrderBy(g => g.Key))
            {
                writer.WriteLine($"  [{group.Key}]");
                foreach (var bucket in group.Value.OrderByDescending(b => b.Value.Count))
                {
                    string examples = string.Join(" | ", bucket.Value.Examples);
                    writer.WriteLine($"    {bucket.Key}: {bucket.Value.Count}\te.g. {examples}");
                }
            }
        }

        foreach (var sub in rule.SubRules)
            WriteRule(writer, sub, fullPath);
    }
}
