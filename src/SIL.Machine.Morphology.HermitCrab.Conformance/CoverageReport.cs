using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Emits the coverage tables described in docs/conformance-language-suite-plan.md section 4:
/// <c>conformance/coverage.csv</c> (language, word, parse signature, construct -- one row per
/// parse×construct, expect_fail words getting an empty signature), <c>conformance/rules.csv</c>
/// (language, rule id, exercising words -- semicolon-joined, unverified <c>blocked_by</c>
/// attributions marked with a "!" prefix), and <c>conformance/fixtures.csv</c> (one row per
/// fixture: directory, category, grammar name, and the distinct constructs it exercises -- the
/// generated replacement for a hand-maintained fixture table in README.md, which goes stale the
/// moment a fixture is added). Also does dead-rule detection: every rule id
/// <see cref="GrammarRuleIndex"/> finds in a grammar.xml that no word's "rules:"/"blocked_by" ever
/// names.
///
/// <b>Verified vs. label-only attribution.</b> A "rules:" entry is VERIFIED elsewhere (self-check
/// mode fails the fixture if it doesn't match the actual trace); crash-attributed rules (see
/// <see cref="ObserveCrashAttributedRuleIds"/>) are VERIFIED here, by actually parsing and catching
/// the identified exception. A bare <c>blocked_by</c> label is neither -- <c>WordsYamlLoader</c>
/// only checks it's non-empty on an expect_fail word, never that the named rule actually
/// participated. A rule whose every exercising token is a "!"-prefixed, uncorroborated
/// <c>blocked_by</c> is LABEL-ONLY: not dead (the gate only fails on zero attribution), but the
/// exact citation-without-evidence shape a shortcut could hide behind. <see cref="LabelOnlyRule"/>
/// keeps that countable instead of invisible.
/// </summary>
public static class CoverageReport
{
    public class DeadRule
    {
        public string FixtureId = "";
        public string Language = "";
        public string RuleId = "";
    }

    /// <summary>A rule whose only exercising word(s) are unverified <c>blocked_by</c> labels -- no
    /// word exercises it via a verified channel (a real "rules:" trace hit, or a crash whose
    /// exception carried the rule's identity; see <see cref="ObserveCrashAttributedRuleIds"/>). Not
    /// dead (the dead-rule gate only fails on zero attribution of any kind, verified or not), but this
    /// is exactly the citation-without-evidence shape the gate exists to keep visible: a rule can be
    /// silenced by naming it in a label, and this count is how that stays countable rather than
    /// invisible.</summary>
    public class LabelOnlyRule
    {
        public string FixtureId = "";
        public string Language = "";
        public string RuleId = "";
    }

    public class CoverageResult
    {
        public List<DeadRule> DeadRules { get; } = new();
        public List<LabelOnlyRule> LabelOnlyRules { get; } = new();

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
            if (line.Length == 0 || line.StartsWith("#"))
                continue;
            constructs.Add(line);
        }
        return constructs;
    }

    public static CoverageResult WriteCsvs(
        List<Fixture> fixtures,
        string coverageCsvPath,
        string rulesCsvPath,
        string fixtureIndexCsvPath
    )
    {
        var result = new CoverageResult();
        var coverageRows = new List<(string language, string word, string signature, string construct)>();
        var ruleRows = new List<(string language, string ruleId, string words)>();
        var fixtureRows = new List<(string directory, string category, string grammarName, string exercises)>();

        foreach (Fixture fixture in fixtures)
        {
            string language = fixture.Words.Language;
            GrammarRuleIndex ruleIndex = GrammarRuleIndex.Load(fixture.GrammarPath);
            // fixture.Id is "<category>/<directory-name>" (see Fixture.DiscoverAll) -- the category
            // is always the text before the first '/'.
            string category = fixture.Id.Split('/', 2)[0];
            var fixtureExercises = new SortedSet<string>(StringComparer.Ordinal);

            // word -> rule ids VERIFIED by a caught, rule-identified crash (see
            // ObserveCrashAttributedRuleIds) while actually parsing this fixture's words. Empty for
            // every fixture that doesn't declare expect_crash.
            Dictionary<string, HashSet<string>> crashRuleIdsByWord = ObserveCrashAttributedRuleIds(fixture, ruleIndex);

            // rule id -> exercising word tokens ("word" for a VERIFIED hit -- a real "rules:" trace
            // match, or a crash whose exception carried the rule's identity -- "!word" for a
            // blocked_by attribution with no verified corroboration).
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
                    // expect_fail and expect_skip words both carry word-level exercises and no parses;
                    // only expect_fail words carry blocked_by (a skip is malformed input, not a
                    // rule-forbidden analysis), but iterating the empty list is harmless either way.
                    foreach (string construct in word.Exercises)
                    {
                        coverageRows.Add((language, word.Word, "", construct));
                        result.CoveredConstructs.Add(construct);
                        fixtureExercises.Add(construct);
                    }

                    crashRuleIdsByWord.TryGetValue(word.Word, out HashSet<string> crashRuleIds);
                    IEnumerable<string> ruleIdsForWord = word.BlockedBy;
                    if (crashRuleIds != null)
                        ruleIdsForWord = ruleIdsForWord.Union(crashRuleIds, StringComparer.Ordinal);
                    foreach (string ruleId in ruleIdsForWord)
                    {
                        bool verified = crashRuleIds != null && crashRuleIds.Contains(ruleId);
                        AddExercise(ruleId, verified ? word.Word : "!" + word.Word);
                    }
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
                            fixtureExercises.Add(construct);
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
                    // Every token is "!"-prefixed (blocked_by, unverified) and none is a plain
                    // VERIFIED hit -- the rule has attribution (so it isn't dead) but none of it is
                    // evidence, only assertion.
                    if (words.All(w => w.StartsWith("!", StringComparison.Ordinal)))
                    {
                        result.LabelOnlyRules.Add(
                            new LabelOnlyRule
                            {
                                FixtureId = fixture.Id,
                                Language = language,
                                RuleId = ruleId,
                            }
                        );
                    }
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

            fixtureRows.Add(
                (fixture.Id, category, LoadGrammarName(fixture.GrammarPath), string.Join(";", fixtureExercises))
            );
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
        WriteCsv(
            fixtureIndexCsvPath,
            new[] { "directory", "category", "grammar_name", "exercises" },
            fixtureRows.Select(r => new[] { r.directory, r.category, r.grammarName, r.exercises })
        );

        return result;
    }

    /// <summary>
    /// For a fixture declaring <c>expect_crash: true</c>, actually parses each word through the
    /// in-process oracle so a rule identified by a caught <see cref="InfiniteLoopException"/> (its
    /// <see cref="InfiniteLoopException.RuleName"/>) counts as a VERIFIED exerciser of that word --
    /// the same standard a real "rules:" trace hit already meets -- instead of relying solely on the
    /// word's <c>blocked_by</c> label, which asserts the same fact but never checks it (see
    /// <c>WordsYamlLoader</c>'s doc comment). Returns an empty map for a non-crash fixture, or if the
    /// crash reproduces but the exception carries no rule identity, or names a rule this grammar's
    /// index cannot resolve -- callers then fall back to treating the word's blocked_by list as
    /// label-only, which is always a safe default (it can under-attribute, never over-attribute).
    /// </summary>
    private static Dictionary<string, HashSet<string>> ObserveCrashAttributedRuleIds(
        Fixture fixture,
        GrammarRuleIndex ruleIndex
    )
    {
        var observed = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        if (!fixture.Words.ExpectCrash)
            return observed;

        Language language = XmlLanguageLoader.Load(fixture.GrammarPath);
        var morpher = new Morpher(new TraceManager(), language);
        foreach (WordEntry word in fixture.Words.Words)
        {
            try
            {
                morpher.ParseWord(word.Word).ToList();
            }
            catch (InfiniteLoopException ex) when (ex.RuleName != null)
            {
                string ruleId = ruleIndex.ResolveRuleName(ex.RuleName);
                if (ruleId == null)
                    continue;
                if (!observed.TryGetValue(word.Word, out HashSet<string> ruleIds))
                {
                    ruleIds = new HashSet<string>(StringComparer.Ordinal);
                    observed[word.Word] = ruleIds;
                }
                ruleIds.Add(ruleId);
            }
            catch
            {
                // Any other exception (including an InfiniteLoopException with no rule identity)
                // reproduces "a crash happened" but not "which rule" -- nothing to attribute here,
                // so this word's blocked_by (if any) stays label-only.
            }
        }
        return observed;
    }

    /// <summary>Reads a grammar.xml's top-level <c>&lt;Language&gt;&lt;Name&gt;</c> text, for the
    /// fixture index -- independent of <see cref="GrammarRuleIndex"/>, which only walks rule
    /// elements.</summary>
    private static string LoadGrammarName(string grammarPath)
    {
        var settings = new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore };
        using System.Xml.XmlReader reader = System.Xml.XmlReader.Create(grammarPath, settings);
        XDocument doc = XDocument.Load(reader);
        return (string)doc.Root?.Element("Language")?.Element("Name") ?? "";
    }

    private static void WriteCsv(string path, string[] header, IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", header.Select(CsvField)));
        foreach (string[] row in rows)
            sb.AppendLine(string.Join(",", row.Select(CsvField)));
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
