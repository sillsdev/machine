using System;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.V2;

/// <summary>
/// Materializes a v2 fixture (grammar.xml + words.yaml) into the v1 on-disk shape
/// (grammar.xml + words.txt + expected.tsv) in a temp directory, plus an in-memory
/// <see cref="FixtureManifest"/> equivalent to what a hand-authored v1 manifest.json would say --
/// so that a v2 fixture can be run through the existing, unmodified <see cref="Runner"/> +
/// <see cref="AdapterEngine"/> for adapter mode, per
/// docs/conformance-language-suite-plan.md section 2.2 ("The harness materializes words.txt and
/// expected.tsv into a temp directory from words.yaml before invoking an adapter, so existing
/// adapters ... work unmodified"). This is the ONLY v2 code path that touches AdapterEngine/Runner
/// -- everything else about adapter mode is exactly the v1 code, unmodified.
/// </summary>
public static class FixtureMaterializer
{
    /// <summary>
    /// Builds the temp directory and returns a v1 <see cref="Fixture"/> pointing at it. Caller owns
    /// cleanup (delete the returned fixture's <see cref="Fixture.Directory"/> when done).
    /// </summary>
    public static Fixture Materialize(FixtureV2 fixtureV2)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "hc-conformance-v2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        File.Copy(fixtureV2.GrammarPath, Path.Combine(tempDir, "grammar.xml"));

        // Words with any guess:true parse are self-check-only (PROTOCOL.md section 3: BatchCommand
        // has no guessRoot opt-in, so no contract-compliant adapter can ever produce a guess parse)
        // and are omitted from adapter materialization entirely rather than materialized with
        // expectations no adapter can meet.
        var adapterWords = fixtureV2.Words.Words.Where(w => w.Parses.All(p => !p.Guess)).ToList();

        string wordsPath = Path.Combine(tempDir, "words.txt");
        File.WriteAllLines(wordsPath, adapterWords.Select(w => w.Word));

        string expectedPath = Path.Combine(tempDir, "expected.tsv");
        using (var writer = new StreamWriter(expectedPath))
        {
            if (fixtureV2.Words.ExpectCrash)
            {
                // v1's own expectCrash convention (see conformance/PROTOCOL.md's "STARTED sentinel"
                // section): only the STARTED sentinel for the one word that crashes the oracle, no
                // real result row -- Runner never reads this content for an expectCrash fixture
                // (its crash-handling catch block bypasses the diff entirely), but SignatureTsv.ReadFile
                // still needs *a* file to exist.
                WordEntry first = fixtureV2.Words.Words[0];
                writer.WriteLine($"0\t{first.Word}\tSTARTED");
            }
            else
            {
                int i = 0;
                foreach (WordEntry word in adapterWords)
                {
                    string signature = BuildExpectedSignature(word);
                    // expect_skip words carry status "SKIPPED" (the oracle throws InvalidShapeException),
                    // matching v1's expected.tsv row shape "N word 0 SKIPPED -"; everything else is "ok".
                    string status = word.ExpectSkip ? "SKIPPED" : "ok";
                    writer.WriteLine($"{i}\t{word.Word}\t0\t{status}\t{signature}");
                    i++;
                }
            }
        }

        var manifest = new FixtureManifest
        {
            Id = fixtureV2.Id,
            Category = fixtureV2.Words.BudgetMs.HasValue ? "pathological" : "single-feature",
            Requires = fixtureV2.Words.Requires,
            ExpectCrash = fixtureV2.Words.ExpectCrash,
            Budget = fixtureV2.Words.BudgetMs.HasValue
                ? new FixtureBudget { WallClockMs = fixtureV2.Words.BudgetMs.Value }
                : null,
            Provenance = string.Join(
                "; ",
                fixtureV2.Words.Words.Where(w => !string.IsNullOrEmpty(w.Provenance)).Select(w => w.Provenance)
            ),
        };

        return new Fixture(tempDir, manifest);
    }

    public static void Cleanup(Fixture materialized)
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
