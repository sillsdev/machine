using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Coverage and dead-rule measurement over the fixtures.
///
/// <b><c>blocked_by</c> is asserted, not verified.</b> A rule named in an expect_fail word's
/// <c>blocked_by</c> is credited as exercised, but nothing checks the trace to confirm that rule
/// actually blocked the word. The only check is that every label is a rule id declared in that
/// fixture's grammar (<see cref="FindUnknownBlockedBy"/>), which catches typos and stale ids, not
/// a label naming a real rule that never ran. Verifying labels against the parse trace is tracked
/// in sillsdev/machine#552.
/// </summary>
public static class CoverageReport
{
    public class DeadRule
    {
        public string FixtureId = "";
        public string Language = "";
        public string RuleId = "";
    }

    public class UnknownBlockedBy
    {
        public string FixtureId = "";
        public string Word = "";
        public string Label = "";
    }

    public class CoverageResult
    {
        public List<DeadRule> DeadRules { get; } = new();

        /// <summary><c>blocked_by</c> labels that are not a rule id in their fixture's grammar.</summary>
        public List<UnknownBlockedBy> UnknownBlockedBy { get; } = new();

        /// <summary>Every construct string any word exercises (the distinct <c>construct</c> column
        /// of coverage.csv). Diffed against <c>constructs.txt</c> by the caller to report absolute
        /// construct coverage.</summary>
        public HashSet<string> CoveredConstructs { get; } = new(StringComparer.Ordinal);
    }

    public static List<string> LoadConstructChecklist(string path)
    {
        var constructs = new List<string>();
        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;
            constructs.Add(line);
        }
        return constructs;
    }

    /// <summary>Reads <c>dead-rules-allowed.tsv</c>: one <c>fixture-id TAB rule-id TAB reason</c> row
    /// per rule a fixture declares on purpose without any word exercising it. The reason column is
    /// required so that no rule is allowed without saying why.</summary>
    public static Dictionary<(string FixtureId, string RuleId), string> LoadDeadRuleAllowlist(string path)
    {
        var allowed = new Dictionary<(string, string), string>();
        int lineNumber = 0;
        foreach (string rawLine in File.ReadAllLines(path))
        {
            lineNumber++;
            if (rawLine.Trim().Length == 0 || rawLine.StartsWith("#", StringComparison.Ordinal))
                continue;
            string[] columns = rawLine.Split('\t');
            if (columns.Length != 3 || columns.Any(c => c.Trim().Length == 0))
                throw new FormatException($"{path}:{lineNumber}: expected fixture, rule and reason separated by tabs");
            if (!allowed.TryAdd((columns[0].Trim(), columns[1].Trim()), columns[2].Trim()))
                throw new FormatException($"{path}:{lineNumber}: duplicate entry");
        }
        return allowed;
    }

    /// <summary>Splits the measured dead rules against the allowlist. <c>Unexpected</c> are dead rules
    /// nobody has accepted; <c>Stale</c> are accepted rules that some word now exercises, whose entry
    /// must go so the list cannot drift into accepting rules that are no longer dead.</summary>
    public static (List<DeadRule> Unexpected, List<(string FixtureId, string RuleId)> Stale) CheckDeadRules(
        IReadOnlyCollection<DeadRule> deadRules,
        IReadOnlyDictionary<(string FixtureId, string RuleId), string> allowed
    )
    {
        List<DeadRule> unexpected = deadRules.Where(d => !allowed.ContainsKey((d.FixtureId, d.RuleId))).ToList();
        var dead = new HashSet<(string, string)>(deadRules.Select(d => (d.FixtureId, d.RuleId)));
        List<(string, string)> stale = allowed
            .Keys.Where(k => !dead.Contains(k))
            .OrderBy(k => k.FixtureId, StringComparer.Ordinal)
            .ThenBy(k => k.RuleId, StringComparer.Ordinal)
            .ToList();
        return (unexpected, stale);
    }

    /// <summary>Returns every <c>blocked_by</c> label, on an expect_fail or expect_skip word, that is not
    /// in <paramref name="ruleIds"/> (the fixture grammar's declared rule ids). Ordinal comparison.</summary>
    public static List<UnknownBlockedBy> FindUnknownBlockedBy(
        string fixtureId,
        WordsYaml words,
        IReadOnlyCollection<string> ruleIds
    )
    {
        var known = new HashSet<string>(ruleIds, StringComparer.Ordinal);
        var unknown = new List<UnknownBlockedBy>();
        foreach (WordEntry word in words.Words)
        {
            if (!word.ExpectFail && !word.ExpectSkip)
                continue;
            foreach (string label in word.BlockedBy)
            {
                if (!known.Contains(label))
                {
                    unknown.Add(
                        new UnknownBlockedBy
                        {
                            FixtureId = fixtureId,
                            Word = word.Word,
                            Label = label,
                        }
                    );
                }
            }
        }
        return unknown;
    }

    public static CoverageResult WriteCsvs(List<Fixture> fixtures, string coverageCsvPath, string rulesCsvPath)
    {
        var result = new CoverageResult();
        var coverageRows = new List<(string language, string word, string signature, string construct)>();
        var ruleRows = new List<(string language, string ruleId, string words)>();

        foreach (Fixture fixture in fixtures)
        {
            string language = fixture.Words.Language;
            GrammarRuleIndex ruleIndex = GrammarRuleIndex.Load(fixture.GrammarPath);
            result.UnknownBlockedBy.AddRange(FindUnknownBlockedBy(fixture.Id, fixture.Words, ruleIndex.AllRuleIds));

            // rule id -> exercising word tokens ("word" for a positive rules: hit, "!word" for a
            // blocked_by attribution on an expect_fail word).
            var exercisingWords = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            void AddExercise(string ruleId, string token)
            {
                if (!exercisingWords.TryGetValue(ruleId, out List<string> list))
                {
                    list = new List<string>();
                    exercisingWords[ruleId] = list;
                }
                list.Add(token);
            }

            foreach (WordEntry word in fixture.Words.Words)
            {
                if (word.ExpectFail || word.ExpectSkip)
                {
                    foreach (string construct in word.Exercises)
                    {
                        coverageRows.Add((language, word.Word, "", construct));
                        result.CoveredConstructs.Add(construct);
                    }
                    foreach (string ruleId in word.BlockedBy)
                        AddExercise(ruleId, "!" + word.Word);
                }
                else
                {
                    foreach (ParseEntry parse in word.Parses)
                    {
                        IEnumerable<string> constructs = parse
                            .Exercises.Concat(word.Exercises)
                            .Distinct(StringComparer.Ordinal);
                        foreach (string construct in constructs)
                        {
                            coverageRows.Add((language, word.Word, parse.Signature, construct));
                            result.CoveredConstructs.Add(construct);
                        }
                        foreach (string ruleId in parse.Rules)
                            AddExercise(ruleId, word.Word);
                    }
                }
            }

            foreach (
                string ruleId in ruleIndex
                    .AllRuleIds.Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
            )
            {
                if (exercisingWords.TryGetValue(ruleId, out List<string> words))
                {
                    ruleRows.Add((language, ruleId, string.Join(";", words.Distinct(StringComparer.Ordinal))));
                }
                else
                {
                    ruleRows.Add((language, ruleId, ""));
                    result.DeadRules.Add(
                        new DeadRule
                        {
                            FixtureId = fixture.Id,
                            Language = language,
                            RuleId = ruleId,
                        }
                    );
                }
            }
        }

        WriteCsv(
            coverageCsvPath,
            new[] { "language", "word", "signature", "construct" },
            coverageRows.Select(r => new[] { r.language, r.word, r.signature, r.construct })
        );
        WriteCsv(
            rulesCsvPath,
            new[] { "language", "rule_id", "words" },
            ruleRows.Select(r => new[] { r.language, r.ruleId, r.words })
        );

        return result;
    }

    private static void WriteCsv(string path, string[] header, IEnumerable<string[]> rows)
    {
        // Always \n, so the checked-in CSVs compare clean on every platform (local_check.sh diffs them).
        var sb = new StringBuilder();
        sb.Append(string.Join(",", header.Select(CsvField))).Append('\n');
        foreach (string[] row in rows)
            sb.Append(string.Join(",", row.Select(CsvField))).Append('\n');
        File.WriteAllText(path, sb.ToString());
    }

    private static string CsvField(string value)
    {
        value ??= "";
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
