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
/// (language, rule id, exercising words -- semicolon-joined, <c>blocked_by</c> attributions marked
/// with a "!" prefix), and <c>conformance/fixtures.csv</c> (one row per fixture: directory,
/// category, grammar name, and the distinct constructs it exercises -- the generated replacement
/// for a hand-maintained fixture table in README.md, which goes stale the moment a fixture is
/// added). Also does dead-rule detection: every rule id <see cref="GrammarRuleIndex"/> finds in a
/// grammar.xml that no word's "rules:"/"blocked_by" ever names.
/// </summary>
public static class CoverageReport
{
    public class DeadRule
    {
        public string FixtureId = "";
        public string Language = "";
        public string RuleId = "";
    }

    public class CoverageResult
    {
        public List<DeadRule> DeadRules { get; } = new();

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
                    // expect_fail and expect_skip words both carry word-level exercises and no parses;
                    // only expect_fail words carry blocked_by (a skip is malformed input, not a
                    // rule-forbidden analysis), but iterating the empty list is harmless either way.
                    foreach (string construct in word.Exercises)
                    {
                        coverageRows.Add((language, word.Word, "", construct));
                        result.CoveredConstructs.Add(construct);
                        fixtureExercises.Add(construct);
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
