using System;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Materializes a fixture (grammar.xml + words.yaml) into the on-disk adapter shape
/// (grammar.xml + words.txt + expected.tsv) in a temp directory, plus an in-memory
/// <see cref="FixtureManifest"/>, so it can be run through <see cref="MaterializedRunner"/> +
/// <see cref="AdapterEngine"/> for adapter mode, per docs/conformance-language-suite-plan.md
/// section 2.2 ("The harness materializes words.txt and expected.tsv into a temp directory from
/// words.yaml before invoking an adapter, so existing adapters ... work unmodified"). This is the
/// only code path that touches AdapterEngine/MaterializedRunner.
/// </summary>
public static class FixtureMaterializer
{
    /// <summary>
    /// Builds the temp directory and returns a <see cref="MaterializedFixture"/> pointing at it.
    /// Caller owns cleanup (delete the returned fixture's <see cref="MaterializedFixture.Directory"/>
    /// when done).
    /// </summary>
    public static MaterializedFixture Materialize(Fixture fixture)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "hc-conformance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        File.Copy(fixture.GrammarPath, Path.Combine(tempDir, "grammar.xml"));

        // Words with any guess:true parse are self-check-only (PROTOCOL.md section 3: BatchCommand
        // has no guessRoot opt-in, so no contract-compliant adapter can ever produce a guess parse)
        // and are omitted from adapter materialization entirely rather than materialized with
        // expectations no adapter can meet.
        var adapterWords = fixture.Words.Words.Where(w => w.Parses.All(p => !p.Guess)).ToList();

        string wordsPath = Path.Combine(tempDir, "words.txt");
        File.WriteAllLines(wordsPath, adapterWords.Select(w => w.Word));

        string expectedPath = Path.Combine(tempDir, "expected.tsv");
        using (var writer = new StreamWriter(expectedPath))
        {
            if (fixture.Words.ExpectCrash)
            {
                // See conformance/PROTOCOL.md's "STARTED sentinel" section: only the STARTED
                // sentinel for the one word that crashes the oracle, no real result row --
                // MaterializedRunner never reads this content for an expectCrash fixture (its
                // crash-handling catch block bypasses the diff entirely), but
                // SignatureTsv.ReadFile still needs *a* file to exist.
                WordEntry first = fixture.Words.Words[0];
                writer.WriteLine($"0\t{first.Word}\tSTARTED");
            }
            else
            {
                int i = 0;
                foreach (WordEntry word in adapterWords)
                {
                    string signature = BuildExpectedSignature(word);
                    // expect_skip words carry status "SKIPPED" (the oracle throws InvalidShapeException),
                    // matching the expected.tsv row shape "N word 0 SKIPPED -"; everything else is "ok".
                    string status = word.ExpectSkip ? "SKIPPED" : "ok";
                    writer.WriteLine($"{i}\t{word.Word}\t0\t{status}\t{signature}");
                    i++;
                }
            }
        }

        var manifest = new FixtureManifest
        {
            Id = fixture.Id,
            Category = fixture.Words.BudgetMs.HasValue ? "pathological" : "single-feature",
            Requires = fixture.Words.Requires,
            ExpectCrash = fixture.Words.ExpectCrash,
            Budget = fixture.Words.BudgetMs.HasValue
                ? new FixtureBudget { WallClockMs = fixture.Words.BudgetMs.Value }
                : null,
            Provenance = string.Join(
                "; ",
                fixture.Words.Words.Where(w => !string.IsNullOrEmpty(w.Provenance)).Select(w => w.Provenance)
            ),
        };

        return new MaterializedFixture(tempDir, manifest);
    }

    public static void Cleanup(MaterializedFixture materialized)
    {
        try
        {
            if (Directory.Exists(materialized.Directory))
                Directory.Delete(materialized.Directory, recursive: true);
        }
        catch (IOException) { }
    }

    /// <summary>The signature string materialized into expected.tsv column 5: "-" for a word with zero expected parses (expect_fail), else the ";"-joined, ordinally-sorted set of parses[].signature -- the same shape BatchCommand.BuildSignature itself produces.</summary>
    public static string BuildExpectedSignature(WordEntry word)
    {
        if (word.ExpectFail || word.ExpectSkip || word.Parses.Count == 0)
            return "-";
        return string.Join(";", word.Parses.Select(p => p.Signature).OrderBy(s => s, StringComparer.Ordinal));
    }
}
