using System;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance;

/// <summary>
/// Materializes a fixture (grammar.xml + words.yaml) into the on-disk adapter shape
/// (grammar.xml + words.txt + expected.tsv) in a temp directory, plus an in-memory
/// <see cref="FixtureManifest"/>, so it can be run through <see cref="MaterializedRunner"/> +
/// <see cref="AdapterEngine"/> for adapter mode without either needing to know about the
/// words.yaml format. This is the only code path that touches AdapterEngine/MaterializedRunner.
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
        return Materialize(fixture, new WordRun("default", fixture.Words.Morpher, fixture.Words.Words));
    }

    public static MaterializedFixture Materialize(Fixture fixture, WordRun run)
    {
        var adapterWords = run.Morpher.GuessRoots.HasValue
            ? run.Words
            : run.Words.Where(w => w.Parses.All(p => !p.Guess)).ToList();
        if (run.Name != "default" && adapterWords.Count == 0)
        {
            throw new NamedVariantHasNoAdapterVisibleWordsException(
                fixture.Id,
                run.Name,
                "no adapter-visible words remain after legacy guess filtering; set morpher.GuessRoots explicitly "
                    + "if the variant is intended to include guessed-word rows"
            );
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "hc-conformance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        File.Copy(fixture.GrammarPath, Path.Combine(tempDir, "grammar.xml"));

        string wordsPath = Path.Combine(tempDir, "words.txt");
        File.WriteAllLines(wordsPath, adapterWords.Select(w => w.Word));

        string expectedPath = Path.Combine(tempDir, "expected.tsv");
        using (var writer = new StreamWriter(expectedPath))
        {
            if (fixture.Words.ExpectCrash)
            {
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

        File.WriteAllText(
            Path.Combine(tempDir, MaterializedFixture.MorpherFileName),
            System.Text.Json.JsonSerializer.Serialize(run.Morpher.ToDictionary())
        );

        var manifest = new FixtureManifest
        {
            Id = fixture.Id,
            Category = fixture.Words.BudgetMs.HasValue ? "pathological" : "single-feature",
            Requires = fixture.Words.Requires,
            Morpher = run.Morpher,
            ExpectCrash = fixture.Words.ExpectCrash,
            Budget = fixture.Words.BudgetMs.HasValue
                ? new FixtureBudget { WallClockMs = fixture.Words.BudgetMs.Value }
                : null,
            Provenance = string.Join(
                "; ",
                run.Words.Where(w => !string.IsNullOrEmpty(w.Provenance)).Select(w => w.Provenance)
            ),
        };

        bool requiresMorpherSettings = RequiresDerivation
            .Derive(fixture.GrammarPath, fixture.Words.Runs.Select(candidate => candidate.Morpher))
            .Contains(MorpherSettings.Capability, StringComparer.Ordinal);
        return new MaterializedFixture(
            tempDir,
            manifest,
            run.Name == "default" ? null : run.Name,
            requiresMorpherSettings
        );
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

    /// <summary>The signature string materialized into expected.tsv column 5: "-" for a word with zero expected
    /// parses (expect_fail), else the ";"-joined, ordinally-sorted set of parses[].signature -- the same shape
    /// BatchCommand.BuildSignature itself produces.</summary>
    public static string BuildExpectedSignature(WordEntry word)
    {
        if (word.ExpectFail || word.ExpectSkip || word.Parses.Count == 0)
            return "-";
        return string.Join(";", word.Parses.Select(p => p.Signature).OrderBy(s => s, StringComparer.Ordinal));
    }
}

internal sealed class NamedVariantHasNoAdapterVisibleWordsException(string fixtureId, string variantName, string reason)
    : InvalidOperationException($"fixture '{fixtureId}' variant '{variantName}' {reason}.") { }
