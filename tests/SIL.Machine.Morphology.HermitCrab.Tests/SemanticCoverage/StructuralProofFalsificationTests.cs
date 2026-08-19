#nullable enable
using System.Diagnostics;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.Machine.Morphology.HermitCrab.Conformance;
using SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

namespace SIL.Machine.Morphology.HermitCrab;

/// <summary>
/// Falsifies the structural-proof contract against the REAL corpus. Every proof kind
/// (<see cref="OrderingProofs"/>, <see cref="UnorderedInvariantProofs"/>, <see cref="InactiveMemberProofs"/>,
/// <see cref="PosDisjointProofs"/>, <see cref="TemplateMaskedProofs"/>, <see cref="NeverFiresProofs"/>,
/// <see cref="FeatureValueDisjointProofs"/>) asserts that an item can NEVER show a counterfactual delta. So an item
/// that is BOTH certified by one of them AND empirically evidenced by an actual engine run is a
/// contradiction in the proof kind, not a tie to break -- see <see cref="CoverageCompletenessGate"/>'s
/// <c>Conflicting</c> resolution, which this test exercises against the real corpus rather than a
/// hand-built input.
///
/// Deliberately does not touch <see cref="CounterfactualGate"/>, <see cref="CounterfactualLedger"/>, or
/// <see cref="Program"/> -- those are owned by concurrent work -- and runs its own child
/// <c>hc-conformance.dll --evaluate-mutant</c> process per baseline/swap, one at a time, sequentially,
/// exactly as <see cref="OrderingGenerator"/>'s own design intends. Proof-kind certification itself is
/// pure XML analysis (no engine, no process) and costs nothing extra.
/// </summary>
[TestFixture]
public sealed class StructuralProofFalsificationTests
{
    private static string RepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory, "conformance", "constructs.txt")))
                return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate the repository root.");
        return string.Empty;
    }

    private enum Certification
    {
        None,
        DisjointDomains,
        UnorderedInvariant,
        InactiveMember,
        PosDisjoint,
        TemplateMasked,
        NeverFires,
        FeatureValueDisjoint,
    }

    private sealed record ItemOutcome(
        OrderingItem Item,
        Certification Certification,
        string? ProofReason,
        bool Evidenced,
        string? DiffWord,
        string? Before,
        string? After
    );

    // 30s, matching OrderingCounterfactualMeasurementTests: this run's child processes carry no
    // in-process fallback, so a genuinely slow (not hung) mutant must not be misreported as a timeout.
    private static readonly TimeSpan EvaluationTimeout = TimeSpan.FromSeconds(30);

    [Test]
    [Explicit(
        "Falsification run, not a CI gate: shells out to hc-conformance.dll once per fixture baseline "
            + "plus once per one of the 138 ordering adjacent-swap items (~166 child processes total, "
            + "sequential, each independently killable on timeout). Takes on the order of a few minutes; "
            + "run manually."
    )]
    public void CertifiedItemsMustNeverShowAnEmpiricalDelta()
    {
        string root = RepositoryRoot();
        string dllPath = Path.Combine(
            root,
            "src",
            "SIL.Machine.Morphology.HermitCrab.Conformance",
            "bin",
            "Debug",
            "net10.0",
            "hc-conformance.dll"
        );
        Assert.That(
            File.Exists(dllPath),
            Is.True,
            $"build the Conformance project first (dotnet build): {dllPath} not found"
        );

        string scratchRoot =
            Environment.GetEnvironmentVariable("FALSIFICATION_SCRATCH")
            ?? Path.Combine(Path.GetTempPath(), "hc-structural-proof-falsification");
        if (Directory.Exists(scratchRoot))
            Directory.Delete(scratchRoot, recursive: true);
        Directory.CreateDirectory(scratchRoot);

        List<Fixture> fixtures = Fixture.DiscoverAll(Path.Combine(root, "conformance"));
        var outcomes = new List<ItemOutcome>();
        int totalPairsSeen = 0;
        var wallClock = Stopwatch.StartNew();

        foreach (Fixture fixture in fixtures)
        {
            XDocument grammar = XDocument.Load(fixture.GrammarPath);
            IReadOnlyList<OrderingItem> items = OrderingGenerator.EnumerateAdjacentPairs(grammar, fixture.Id);
            totalPairsSeen += items.Count;
            if (items.Count == 0)
                continue;

            string[] words = fixture.Words.Words.Select(w => w.Word).ToArray();
            string wordsPath = Path.Combine(scratchRoot, $"words-{Sanitize(fixture.Id)}.txt");
            File.WriteAllLines(wordsPath, words);

            TestContext.Out.WriteLine($"[{fixture.Id}] {items.Count} item(s), {words.Length} word(s)");

            IReadOnlyList<string>? baseline;
            try
            {
                baseline = RunEvaluateMutant(dllPath, fixture.GrammarPath, wordsPath, EvaluationTimeout);
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"  *** BASELINE ITSELF FAILED: {ex.GetType().Name}: {ex.Message} ***");
                baseline = null;
            }

            foreach (OrderingItem item in items)
            {
                Certification certification = Certify(grammar, item, out string? proofReason);

                if (baseline is null)
                {
                    // The fixture's own baseline did not evaluate cleanly. Not evidence either way, so
                    // it is neither Evidenced nor safe to certify against -- record and move on rather
                    // than guessing what a comparison against a failed baseline would even mean.
                    outcomes.Add(new ItemOutcome(item, certification, proofReason, false, null, null, null));
                    continue;
                }

                OrderingSwap? swap = OrderingGenerator.Swap(grammar, item);
                Assert.That(swap, Is.Not.Null, $"item {item.Id} did not match its own fixture's grammar");
                string mutatedPath = Path.Combine(scratchRoot, $"mutant-{Sanitize(item.Id)}.xml");
                swap!.Mutated.Save(mutatedPath);

                try
                {
                    IReadOnlyList<string> mutated = RunEvaluateMutant(
                        dllPath,
                        mutatedPath,
                        wordsPath,
                        EvaluationTimeout
                    );

                    int diffIndex = -1;
                    for (int i = 0; i < words.Length && i < baseline.Count && i < mutated.Count; i++)
                    {
                        if (baseline[i] != mutated[i])
                        {
                            diffIndex = i;
                            break;
                        }
                    }

                    bool countMismatch = baseline.Count != mutated.Count;
                    bool evidenced = diffIndex >= 0 || countMismatch;
                    outcomes.Add(
                        new ItemOutcome(
                            item,
                            certification,
                            proofReason,
                            evidenced,
                            diffIndex >= 0 ? words[diffIndex] : (countMismatch ? "<outcome-count-mismatch>" : null),
                            diffIndex >= 0 ? baseline[diffIndex] : null,
                            diffIndex >= 0 ? mutated[diffIndex] : null
                        )
                    );
                }
                catch (TimeoutException tex)
                {
                    TestContext.Out.WriteLine($"  *** TIMEOUT *** {item.Id}: {tex.Message}");
                    // A timeout is "I could not look", never evidence and never safe to treat as
                    // matching a proof's claim either -- record as unresolved (not evidenced).
                    outcomes.Add(new ItemOutcome(item, certification, proofReason, false, null, null, null));
                }
                catch (Exception ex)
                {
                    // A load failure IS a counterfactual delta and must count as evidenced here, exactly
                    // as CounterfactualVerdict.RequiredByDtd/RequiredByLoader do for a Surface item.
                    TestContext.Out.WriteLine($"  *** LOAD FAILURE *** {item.Id}: {ex.GetType().Name}: {ex.Message}");
                    outcomes.Add(
                        new ItemOutcome(
                            item,
                            certification,
                            proofReason,
                            true,
                            "<load-failure>",
                            null,
                            $"{ex.GetType().Name}: {ex.Message}"
                        )
                    );
                }
            }
        }

        wallClock.Stop();
        Assert.That(
            totalPairsSeen,
            Is.EqualTo(138),
            "the corpus-wide pair count must match the design doc's measured figure"
        );
        Assert.That(outcomes, Has.Count.EqualTo(138));

        WriteTally(outcomes, wallClock.Elapsed);

        // The headline check. A structural proof claims an item can NEVER show a delta; empirical
        // evidence that it just did is a contradiction in the proof kind, not a tie to break.
        ItemOutcome[] contradictions = outcomes
            .Where(o => o.Certification != Certification.None && o.Evidenced)
            .ToArray();
        if (contradictions.Length != 0)
        {
            string detail = string.Join(
                "\n",
                contradictions.Select(o =>
                    $"  {o.Item.Id}\n"
                    + $"    certified by: {o.Certification} ({o.ProofReason})\n"
                    + $"    differing word: '{o.DiffWord}': {o.Before} -> {o.After}"
                )
            );
            Assert.Fail(
                $"{contradictions.Length} item(s) are BOTH structurally certified AND empirically evidenced -- "
                    + $"the proof kind is UNSOUND on the real corpus:\n{detail}"
            );
        }
    }

    /// <summary>
    /// Certifies <paramref name="item"/> using the SAME fallback order
    /// <see cref="CoverageEvidencePipeline.BuildProofs"/> uses, so this test's certification matches what
    /// the real pipeline would record rather than a re-invented ordering.
    /// </summary>
    private static Certification Certify(XDocument grammar, OrderingItem item, out string? reason)
    {
        Proof? disjointDomains = OrderingProofs.TryBuild(grammar, item);
        if (disjointDomains is not null)
        {
            reason = disjointDomains.Check;
            return Certification.DisjointDomains;
        }

        Proof? unorderedInvariant = UnorderedInvariantProofs.TryBuild(grammar, item);
        if (unorderedInvariant is not null)
        {
            reason = unorderedInvariant.Check;
            return Certification.UnorderedInvariant;
        }

        Proof? inactiveMember = InactiveMemberProofs.TryBuild(grammar, item);
        if (inactiveMember is not null)
        {
            reason = inactiveMember.Check;
            return Certification.InactiveMember;
        }

        Proof? posDisjoint = PosDisjointProofs.TryBuild(grammar, item);
        if (posDisjoint is not null)
        {
            reason = posDisjoint.Check;
            return Certification.PosDisjoint;
        }

        Proof? templateMasked = TemplateMaskedProofs.TryBuild(grammar, item);
        if (templateMasked is not null)
        {
            reason = templateMasked.Check;
            return Certification.TemplateMasked;
        }

        Proof? neverFires = NeverFiresProofs.TryBuild(grammar, item);
        if (neverFires is not null)
        {
            reason = neverFires.Check;
            return Certification.NeverFires;
        }

        Proof? featureValueDisjoint = FeatureValueDisjointProofs.TryBuild(grammar, item);
        if (featureValueDisjoint is not null)
        {
            reason = featureValueDisjoint.Check;
            return Certification.FeatureValueDisjoint;
        }

        reason = null;
        return Certification.None;
    }

    private static string Sanitize(string id)
    {
        char[] chars = id.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
                chars[i] = '_';
        }
        return new string(chars);
    }

    /// <summary>
    /// Runs one <c>--evaluate-mutant</c> child process: <c>DOTNET_PROCESSOR_COUNT=1 dotnet
    /// hc-conformance.dll --evaluate-mutant &lt;grammar&gt; &lt;words&gt;</c>, one word per line, never
    /// words.yaml. A separate implementation from <c>CounterfactualGate</c>'s private
    /// <c>OutcomesWithTimeout</c> (that file is owned by concurrent work) but the same shape: drain both
    /// pipes before waiting so a full one never deadlocks the wait, kill the whole tree on timeout.
    /// </summary>
    private static IReadOnlyList<string> RunEvaluateMutant(
        string dllPath,
        string grammarPath,
        string wordsPath,
        TimeSpan timeout
    )
    {
        var start = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(dllPath);
        start.ArgumentList.Add("--evaluate-mutant");
        start.ArgumentList.Add(grammarPath);
        start.ArgumentList.Add(wordsPath);
        start.EnvironmentVariables["DOTNET_PROCESSOR_COUNT"] = "1";

        ChildProcessHarness.Result result;
        try
        {
            result = ChildProcessHarness.Run(start, CancellationToken.None, backstop: timeout);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"'{grammarPath}' did not complete within {timeout.TotalSeconds:0}s");
        }

        if (result.ExitCode != 0)
        {
            string error = result.StandardError.Trim();
            throw new InvalidOperationException(error.Length != 0 ? error : $"exit code {result.ExitCode}");
        }

        return result
            .StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r'))
            .ToArray();
    }

    private static void WriteTally(List<ItemOutcome> outcomes, TimeSpan wallClock)
    {
        TestContext.Out.WriteLine();
        TestContext.Out.WriteLine("STRUCTURAL PROOF FALSIFICATION -- 138-ITEM TALLY");
        TestContext.Out.WriteLine("==================================================");
        TestContext.Out.WriteLine($"wall clock: {wallClock.TotalSeconds:0.0}s");
        TestContext.Out.WriteLine();

        int evidenced = outcomes.Count(o => o.Evidenced);
        TestContext.Out.WriteLine($"evidenced (real delta from the engine): {evidenced}");
        foreach (Certification kind in Enum.GetValues<Certification>())
        {
            if (kind == Certification.None)
                continue;
            int count = outcomes.Count(o => o.Certification == kind);
            TestContext.Out.WriteLine($"certified by {kind}: {count}");
        }
        int uncertifiedAndUnevidenced = outcomes.Count(o => o.Certification == Certification.None && !o.Evidenced);
        TestContext.Out.WriteLine($"open (neither evidenced nor certified): {uncertifiedAndUnevidenced}");
        TestContext.Out.WriteLine();

        TestContext.Out.WriteLine("evidenced items:");
        foreach (ItemOutcome o in outcomes.Where(o => o.Evidenced).OrderBy(o => o.Item.Id, StringComparer.Ordinal))
            TestContext.Out.WriteLine($"  {o.Item.Id}  '{o.DiffWord}': {o.Before} -> {o.After}");
        TestContext.Out.WriteLine();

        TestContext.Out.WriteLine("certified items (by kind):");
        foreach (
            ItemOutcome o in outcomes
                .Where(o => o.Certification != Certification.None)
                .OrderBy(o => o.Certification)
                .ThenBy(o => o.Item.Id, StringComparer.Ordinal)
        )
        {
            TestContext.Out.WriteLine($"  [{o.Certification}] {o.Item.Id}");
        }
    }
}
