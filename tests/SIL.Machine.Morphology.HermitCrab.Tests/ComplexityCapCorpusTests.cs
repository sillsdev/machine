using System.Diagnostics;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Complexity-cap Phase 0 (see complexity-cap.md §7, §9): calibration and no-regression corpus using
/// the real Indonesian/Sena grammars. These grammars + wordlists are large, not licensed for this repo,
/// and stay untracked (see .gitignore) — every test here is [Explicit] (not run by default CI) and
/// skips itself when the files aren't present locally, exactly like the RustifyBenchmark precedent
/// referenced in .gitignore's comment.
/// </summary>
[TestFixture]
[Explicit("Requires the untracked samples/data/{indonesian,sena}-hc.xml corpus; see complexity-cap.md Phase 0.")]
public class ComplexityCapCorpusTests
{
    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "machine.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static (string Grammar, string Words)? FindCorpus(string name)
    {
        string? root = FindRepoRoot();
        if (root == null)
            return null;
        string grammar = Path.Combine(root, "samples", "data", $"{name}-hc.xml");
        string words = Path.Combine(root, "samples", "data", $"{name}-words.txt");
        if (!File.Exists(grammar) || !File.Exists(words))
            return null;
        return (grammar, words);
    }

    // "Unlimited" for calibration purposes only: a genuinely pathological word in a real corpus must
    // not be allowed to hang the calibration run forever (see the Sena run that sat stuck for 23
    // minutes before being killed — exactly the failure mode complexity-cap exists to catch). This is
    // a calibration safety net only, ~2500x above any legitimate word observed so far; it is not a
    // proposed shipped default.
    private const int CalibrationStepCeiling = 50_000_000;

    private static void RunCorpus(string name)
    {
        (string Grammar, string Words)? corpus = FindCorpus(name);
        if (corpus == null)
        {
            Assert.Ignore(
                $"samples/data/{name}-hc.xml and/or {name}-words.txt not present locally (untracked, see .gitignore) — skipping."
            );
            return;
        }

        Language language = XmlLanguageLoader.Load(corpus.Value.Grammar);
        var morpher = new Morpher(new TraceManager(), language)
        {
            MaxParseSteps = CalibrationStepCeiling,
            ParseTimeout = TimeSpan.Zero,
        };

        string[] words = File.ReadAllLines(corpus.Value.Words).Select(w => w.Trim()).Where(w => w.Length > 0).ToArray();

        int maxSteps = 0;
        string maxStepsWord = "";
        var sw = Stopwatch.StartNew();
        long maxWordMs = 0;
        string maxWordMsWord = "";
        int wordsParsed = 0;
        int wordsSkipped = 0;
        var pathologicalWords = new List<(string Word, int Steps)>();
        foreach (string word in words)
        {
            ParseDiagnostics diagnostics;
            var wordSw = Stopwatch.StartNew();
            try
            {
                morpher.ParseWord(word, out _, false, out diagnostics).ToList();
            }
            catch (InvalidShapeException)
            {
                // Malformed/non-word lines in this ad hoc wordlist (e.g. gloss annotations that slipped
                // in) aren't a complexity-cap concern — skip rather than fail the calibration run.
                wordsSkipped++;
                continue;
            }
            wordSw.Stop();
            wordsParsed++;
            // Flushed immediately (unlike TestContext.Out, which buffers until the test ends) so a
            // hang/crash mid-run still shows which word was last attempted.
            TestContext.Progress.WriteLine(
                $"  [{wordsParsed}/{words.Length}] '{word}': {diagnostics.StepsUsed} steps, {wordSw.ElapsedMilliseconds}ms"
            );

            if (diagnostics.BudgetExhausted)
                pathologicalWords.Add((word, diagnostics.StepsUsed));

            if (diagnostics.StepsUsed > maxSteps)
            {
                maxSteps = diagnostics.StepsUsed;
                maxStepsWord = word;
            }
            if (wordSw.ElapsedMilliseconds > maxWordMs)
            {
                maxWordMs = wordSw.ElapsedMilliseconds;
                maxWordMsWord = word;
            }
        }
        sw.Stop();

        TestContext.Out.WriteLine(
            $"{name}: {wordsParsed} words parsed ({wordsSkipped} skipped as malformed), total {sw.ElapsedMilliseconds}ms, "
                + $"max steps {maxSteps} (word '{maxStepsWord}'), "
                + $"max single-word time {maxWordMs}ms (word '{maxWordMsWord}'), "
                + $"suggested default MaxParseSteps (100x observed max) = {Math.Max(maxSteps, 1) * 100}"
        );

        if (pathologicalWords.Count > 0)
        {
            TestContext.Out.WriteLine(
                $"WARNING: {pathologicalWords.Count} word(s) hit the {CalibrationStepCeiling:N0}-step calibration "
                    + "ceiling — these are candidates for genuinely pathological grammar interactions, not "
                    + "legitimate baseline data points:"
            );
            foreach ((string word, int steps) in pathologicalWords)
                TestContext.Out.WriteLine($"  '{word}': {steps} steps (hit ceiling)");
        }

        Assert.That(
            pathologicalWords,
            Is.Empty,
            $"{pathologicalWords.Count} word(s) hit the calibration step ceiling — see output for which word(s); "
                + "investigate with RerunWithDiagnostics before trusting the max-steps number above for calibration."
        );
    }

    [Test]
    public void Indonesian_Baseline_NoWordExhaustsUnlimitedBudget()
    {
        RunCorpus("indonesian");
    }

    /// <summary>
    /// Ad hoc diagnostic, not a pass/fail assertion: reports which rule(s) account for the bulk of the
    /// step count on the single most expensive word in the corpus, using RerunWithDiagnostics exactly
    /// as the "Writing performant HC grammars" guide (docs/hermitcrab-grammar-performance.md)
    /// recommends. Useful for eyeballing whether a corpus's worst-case word is a legitimate expensive
    /// parse or a symptom of a specific bad rule.
    /// </summary>
    [Test]
    public void Indonesian_TopOffendingRules_ForWorstWord()
    {
        ReportTopOffenders("indonesian", "mengamat-amati");
    }

    private static void ReportTopOffenders(string name, string word)
    {
        (string Grammar, string Words)? corpus = FindCorpus(name);
        if (corpus == null)
        {
            Assert.Ignore($"samples/data/{name}-hc.xml not present locally — skipping.");
            return;
        }

        Language language = XmlLanguageLoader.Load(corpus.Value.Grammar);
        var morpher = new Morpher(new TraceManager(), language) { MaxParseSteps = 0, ParseTimeout = TimeSpan.Zero };

        ParseDiagnostics diagnostics;
        try
        {
            diagnostics = morpher.RerunWithDiagnostics(word, out IEnumerable<Word> results);
            results.ToList();
        }
        catch (InvalidShapeException)
        {
            Assert.Ignore($"'{word}' is not a valid shape in the {name} grammar's character set.");
            return;
        }

        TestContext.Out.WriteLine(
            $"{name} '{word}': {diagnostics.StepsUsed} steps, {diagnostics.Elapsed.TotalMilliseconds:F1}ms"
        );
        TestContext.Out.WriteLine("Top rules by application count:");
        foreach ((IHCRule rule, int applications) in diagnostics.TopRules.Take(10))
        {
            double pct = 100.0 * applications / Math.Max(diagnostics.StepsUsed, 1);
            TestContext.Out.WriteLine($"  {applications, 6} ({pct, 5:F1}%)  {rule.GetType().Name} '{rule.Name}'");
        }
    }

    [Test]
    public void Sena_Baseline_NoWordExhaustsUnlimitedBudget()
    {
        RunCorpus("sena");
    }

    /// <summary>
    /// Confirms the *shipped* defaults (Morpher.DefaultMaxParseSteps / DefaultParseTimeout) are
    /// generous enough to be invisible on real, legitimate grammars — the "no-regression" half of
    /// Phase 0 (§7): every word must still complete without tripping the budget at the defaults a
    /// naive consumer gets out of the box.
    /// </summary>
    [Test]
    public void Indonesian_ShippedDefaults_NeverTrip()
    {
        RunCorpusAtDefaults("indonesian");
    }

    [Test]
    public void Sena_ShippedDefaults_NeverTrip()
    {
        RunCorpusAtDefaults("sena");
    }

    private static void RunCorpusAtDefaults(string name)
    {
        (string Grammar, string Words)? corpus = FindCorpus(name);
        if (corpus == null)
        {
            Assert.Ignore(
                $"samples/data/{name}-hc.xml and/or {name}-words.txt not present locally (untracked, see .gitignore) — skipping."
            );
            return;
        }

        Language language = XmlLanguageLoader.Load(corpus.Value.Grammar);
        var morpher = new Morpher(new TraceManager(), language); // shipped defaults

        string[] words = File.ReadAllLines(corpus.Value.Words).Select(w => w.Trim()).Where(w => w.Length > 0).ToArray();

        foreach (string word in words)
        {
            ParseDiagnostics diagnostics;
            try
            {
                morpher.ParseWord(word, out _, false, out diagnostics).ToList();
            }
            catch (InvalidShapeException)
            {
                continue;
            }
            Assert.That(
                diagnostics.BudgetExhausted,
                Is.False,
                $"'{word}' tripped the shipped default budget (StepsUsed={diagnostics.StepsUsed}) — defaults are not generous enough for this corpus"
            );
        }
    }
}
