#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>The states a grammar-observable surface may end in; only the first three are evidence.</summary>
public enum CounterfactualVerdict
{
    /// <summary>Neutralizing the surface changed the result. The delta is the evidence.</summary>
    Evidenced,

    /// <summary>
    /// Neutralizing the surface made <c>XmlLanguageLoader</c>'s own code throw -- an IDREF lookup, a
    /// feature/coercion failure, or similar -- after the document already passed generic XML/DTD
    /// validation. This is a genuine engine-semantic witness: the failure could only happen because
    /// HermitCrab's loader actually consulted the surface. Strictly weaker than <see cref="Evidenced"/>
    /// (no word ever parses to show a delta) but a real parse-time observation, unlike
    /// <see cref="RequiredByDtd"/>.
    /// </summary>
    RequiredByLoader,

    /// <summary>
    /// The surface only changed a result when jointly activated with an independent referencing
    /// declaration; flipping either alone did not. Strictly weaker than <see cref="Evidenced"/> and
    /// <see cref="RequiredByLoader"/> -- it pins the delta on the PAIR, and the partner's own necessity
    /// check (not just the target's) is what the pair's three-run record has to show. Stronger than
    /// <see cref="RequiredByDtd"/>: it still comes from a real three-run parse, not a document that
    /// never reached the engine at all.
    /// </summary>
    EvidencedJointly,

    /// <summary>
    /// Neutralizing the surface removed something the DTD's own content model requires -- an element
    /// whose parent's declared sequence no longer validates, or (in the limit) the document's root, so
    /// the document fails <c>XmlReader</c>'s generic DTD validation before <c>XmlLanguageLoader</c> runs
    /// a single line. This re-derives what Level 1's static DTD enumeration already knows and is
    /// <b>not</b> equally conclusive with <see cref="Evidenced"/>: no HermitCrab-specific code, and no
    /// word, was ever reached. A Rust engine that loads every such grammar and then silently ignores the
    /// construct at parse time is indistinguishable from a correct one on this verdict alone.
    /// </summary>
    RequiredByDtd,

    /// <summary>The mutant did not finish in time. A timing race is not a semantic delta.</summary>
    Timeout,

    /// <summary>Neutralizing the surface changed nothing. Not evidence; fails unless a proof applies.</summary>
    Unobservable,
}

/// <summary>
/// <paramref name="ExampleWord"/>/<paramref name="ExampleOutcome"/>/<paramref name="CounterexampleOutcome"/>
/// are captured structurally at the moment <see cref="CounterfactualGate"/> compares a mutant against
/// the baseline, so a caller building an <see cref="Evidence"/> record never has to regex
/// <paramref name="Delta"/>'s prose. Left at their defaults (null / <see cref="CounterexampleKind.None"/>)
/// for verdicts that are not evidence. <paramref name="WitnessingFixtures"/> is every fixture
/// <see cref="CounterfactualLedger.Sweep"/> found reaching this exact <paramref name="Verdict"/> for
/// <paramref name="SurfaceId"/> -- <paramref name="FixtureId"/> is only the first-discovered of that
/// set, kept for the example fields above, never the only one; null outside <c>Sweep</c>'s own merge,
/// where "which fixture(s)" is not yet a question being asked.
/// </summary>
public sealed record CounterfactualResult(
    string SurfaceId,
    string FixtureId,
    CounterfactualVerdict Verdict,
    string Mutation,
    string Delta,
    string? ExampleWord = null,
    string? ExampleOutcome = null,
    CounterexampleKind CounterexampleKind = CounterexampleKind.None,
    string? CounterexampleOutcome = null,
    IReadOnlyList<string>? WitnessingFixtures = null
);

/// <summary>One word's measured parse time, for diagnosing which fixtures or mutants run slow.</summary>
public sealed record WordTiming(string Phase, string FixtureId, string? SurfaceId, string Word, long ElapsedMs);

/// <summary>
/// Decides whether a surface is load-bearing by running a fixture twice: once as written, once with the
/// surface neutralized. A difference in the parse results is the evidence; no difference means the
/// fixture does not show the surface matters, whatever else it might declare.
/// </summary>
public static class CounterfactualGate
{
    /// <summary>The parse outcome of one word, reduced to what a fixture actually asserts.</summary>
    private static string Outcome(Morpher morpher, string word, Action<string, long>? onTimed)
    {
        try
        {
            (string status, long elapsedMs, string signature) = SignatureFormat.ParseOneWord(morpher, word);
            onTimed?.Invoke(word, elapsedMs);
            return $"{status}::{signature}";
        }
        catch (Exception ex)
        {
            // A mutated grammar may drive the engine into a state the original never reaches. That is a
            // difference in outcome, so it counts as a delta rather than aborting the run.
            return $"threw::{ex.GetType().Name}";
        }
    }

    private static IReadOnlyList<string> Outcomes(
        string grammarPath,
        IReadOnlyList<string> words,
        Action<string, long>? onTimed = null
    )
    {
        Language language = XmlLanguageLoader.Load(grammarPath);
        Morpher morpher = ConformanceMorpherFactory.Create(language);
        return words.Select(word => Outcome(morpher, word, onTimed)).ToArray();
    }

    // A mutated grammar can drop whatever bounded a search, so a mutant may never terminate. Kept wide
    // enough that a merely slow mutant is never mistaken for a non-terminating one. internal so
    // InterfaceWitnessGate shares this constant instead of keeping a second copy that could drift.
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(180);

    // Every Timeout ever investigated here resolved to a real verdict on a quieter machine or under
    // a scoped sweep -- genuine non-termination has never once been observed. A retry therefore
    // protects against nothing the budget does not, and cost 2x on every slow mutant, so the budget
    // carries the whole job: deep-optional-affix-nesting alone needs ~128s uncontended.
    private const int TimeoutConfirmationAttempts = 1;

    /// <summary>Parses every line of <paramref name="wordsPath"/> against a grammar.</summary>
    public static IReadOnlyList<string> EvaluateOneGrammar(
        string grammarPath,
        string wordsPath,
        Action<string, long>? onTimed = null
    ) => Outcomes(grammarPath, File.ReadAllLines(wordsPath).Where(line => line.Length != 0).ToArray(), onTimed);

    // The child's timing side-channel: kept off stdout so it never contaminates the outcome lines a
    // baseline diff compares, and parsed back out of stderr regardless of whether the child succeeded.
    private const string TimingPrefix = "TIME\t";

    private static string ExtractWordTimings(string stderr, Action<string, long>? onTimed)
    {
        var remaining = new List<string>();
        foreach (string rawLine in stderr.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            string[] parts = line.StartsWith(TimingPrefix, StringComparison.Ordinal)
                ? line.Split('\t')
                : Array.Empty<string>();
            if (parts.Length == 3 && long.TryParse(parts[2], out long elapsedMs))
            {
                onTimed?.Invoke(parts[1], elapsedMs);
                continue;
            }
            remaining.Add(line);
        }
        return string.Join('\n', remaining).Trim();
    }

    /// <summary>
    /// Runs the parse in a child process so a mutant that never terminates can be killed. Waiting on an
    /// in-process task only abandons it: it keeps allocating for the rest of the sweep. Requires
    /// <see cref="TimeoutConfirmationAttempts"/> independent timeouts in a row before reporting one --
    /// see that constant's own doc comment for why a single sample is not trusted.
    /// </summary>
    private static IReadOnlyList<string> OutcomesWithTimeout(
        string grammarPath,
        IReadOnlyList<string> words,
        TimeSpan timeout,
        Action<string, long>? onTimed
    )
    {
        TimeoutException lastTimeout;
        int attempt = 1;
        while (true)
        {
            try
            {
                return RunOnceWithTimeout(grammarPath, words, timeout, onTimed);
            }
            catch (TimeoutException ex)
            {
                lastTimeout = ex;
                if (attempt++ >= TimeoutConfirmationAttempts)
                    throw lastTimeout;
            }
        }
    }

    /// <summary>One wall-clock-bounded attempt; see <see cref="TimeoutConfirmationAttempts"/> for why
    /// <see cref="OutcomesWithTimeout"/> never accepts a single one of these as final.</summary>
    private static IReadOnlyList<string> RunOnceWithTimeout(
        string grammarPath,
        IReadOnlyList<string> words,
        TimeSpan timeout,
        Action<string, long>? onTimed
    )
    {
        string wordsPath = grammarPath + ".words";
        File.WriteAllLines(wordsPath, words);
        try
        {
            // Not Environment.ProcessPath / Assembly.GetEntryAssembly(): under `dotnet test` those
            // resolve to the test host, not this assembly, and re-launching the test host with
            // --evaluate-mutant would run the wrong program. This assembly's own location is
            // correct regardless of what process is hosting the caller.
            var start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            start.ArgumentList.Add(typeof(CounterfactualGate).Assembly.Location);
            start.ArgumentList.Add("--evaluate-mutant");
            start.ArgumentList.Add(grammarPath);
            start.ArgumentList.Add(wordsPath);
            // Keep the whole child pinned alongside ConformanceMorpherFactory's sequential engine so
            // any parallel work outside Morpher cannot make a mutant outcome race-dependent.
            start.EnvironmentVariables["DOTNET_PROCESSOR_COUNT"] = "1";

            using System.Diagnostics.Process child =
                System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException("could not start child");

            // Drain both pipes before waiting. Reading one to the end first deadlocks as soon as the
            // child fills the other, and then the timeout below is never reached.
            Task<string> outputTask = child.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit((int)timeout.TotalMilliseconds))
            {
                child.Kill(entireProcessTree: true);
                // Best effort: whatever timings the mutant printed before it was killed are real
                // measurements of the words it did finish, even though the run overall did not.
                if (errorTask.Wait(TimeSpan.FromSeconds(5)))
                    ExtractWordTimings(errorTask.Result, onTimed);
                throw new TimeoutException($"parse did not complete within {timeout.TotalSeconds:0}s");
            }

            string output = outputTask.GetAwaiter().GetResult();
            string error = ExtractWordTimings(errorTask.GetAwaiter().GetResult(), onTimed);
            if (child.ExitCode != 0)
                throw new InvalidOperationException(error.Trim());
            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.TrimEnd('\r'))
                .ToArray();
        }
        finally
        {
            if (File.Exists(wordsPath))
                File.Delete(wordsPath);
        }
    }

    /// <summary>
    /// The fixture's outcomes as written, computed once and shared across every surface it contains --
    /// recomputing it per surface is the dominant cost of a counterfactual sweep.
    /// </summary>
    public static IReadOnlyList<string> ComputeBaseline(Fixture fixture, Action<string, long>? onTimed = null)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        string[] words = fixture.Words.Words.Select(word => word.Word).ToArray();
        return Outcomes(fixture.GrammarPath, words, onTimed);
    }

    /// <summary>
    /// Evaluates <paramref name="grammarPath"/> in the same killable child process a mutant gets, rather
    /// than <see cref="ComputeBaseline"/>'s in-process, untimed path. Needed for a fixture whose baseline
    /// is not trustworthy to run unprotected -- a pathological or expect_crash fixture -- so an Ordering
    /// item belonging to it gets the same non-terminating-search protection a Surface mutant already has.
    /// </summary>
    public static IReadOnlyList<string> EvaluateWithTimeout(
        string grammarPath,
        IReadOnlyList<string> words,
        TimeSpan? timeout = null,
        Action<string, long>? onTimed = null
    ) => OutcomesWithTimeout(grammarPath, words, timeout ?? DefaultTimeout, onTimed);

    /// <summary>
    /// Runs <paramref name="item"/>'s adjacent-pair transposition against the already-computed
    /// <paramref name="baseline"/>, reusing <see cref="RunMutation"/> exactly as a Surface neutralization
    /// does -- the swap is just a different <see cref="GrammarMutation"/> shape (reordering rather than
    /// deleting or rewriting).
    /// </summary>
    public static CounterfactualResult EvaluateOrderingSwap(
        Fixture fixture,
        OrderingItem item,
        IReadOnlyList<string> baseline,
        string scratchDirectory,
        TimeSpan? timeout = null,
        Action<string, long>? onWordTimed = null
    )
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(baseline);
        XDocument grammar = XDocument.Load(fixture.GrammarPath);
        OrderingSwap? swap = OrderingGenerator.Swap(grammar, item);
        if (swap is null)
        {
            return new CounterfactualResult(
                item.Id,
                fixture.Id,
                CounterfactualVerdict.Unobservable,
                "none",
                "the item no longer matches this fixture's grammar"
            );
        }

        var mutation = new GrammarMutation(item.Id, "adjacent-transposition", swap.Detail, swap.Mutated);
        return RunMutation(
            fixture,
            item.Id,
            mutation,
            baseline,
            scratchDirectory,
            timeout ?? DefaultTimeout,
            onWordTimed
        );
    }

    /// <summary>
    /// Runs the fixture with <paramref name="surfaceId"/> neutralized against the already-computed
    /// <paramref name="baseline"/>, and reports which of the four outcomes applies. A deletion that
    /// times out is retried once as a lighter, children-only mutation before being accepted as
    /// pathological: an unconstrained search exploding is a cost of the mutation, not of the surface.
    /// </summary>
    public static CounterfactualResult Evaluate(
        Fixture fixture,
        string surfaceId,
        SemanticInventory inventory,
        IReadOnlyList<string> baseline,
        string scratchDirectory,
        TimeSpan? timeout = null,
        Action<string, long>? onWordTimed = null
    )
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(baseline);
        XDocument grammar = XDocument.Load(fixture.GrammarPath);

        // A single arbitrarily-chosen sibling can be a no-op while another one is not, so every
        // declared sibling is tried before Unobservable is accepted; see EvaluateEveryEnumSibling.
        CounterfactualResult? enumResult = EvaluateEveryEnumSibling(
            fixture,
            surfaceId,
            inventory,
            baseline,
            scratchDirectory,
            timeout ?? DefaultTimeout,
            onWordTimed,
            grammar
        );
        if (enumResult is not null)
            return enumResult;

        GrammarMutation? mutation = GrammarMutator.Mutate(grammar, surfaceId, inventory);
        if (mutation is null)
        {
            return new CounterfactualResult(
                surfaceId,
                fixture.Id,
                CounterfactualVerdict.Unobservable,
                "none",
                "the fixture does not contain this surface"
            );
        }

        TimeSpan effectiveTimeout = timeout ?? DefaultTimeout;
        CounterfactualResult primary = RunMutation(
            fixture,
            surfaceId,
            mutation,
            baseline,
            scratchDirectory,
            effectiveTimeout,
            onWordTimed
        );
        if (primary.Verdict != CounterfactualVerdict.Timeout || mutation.Kind != GrammarMutator.DeletedElements)
            return primary;

        GrammarMutation? lighter = GrammarMutator.MutateByEmptyingElementChildren(grammar, surfaceId);
        if (lighter is null)
            return primary;

        CounterfactualResult fallback = RunMutation(
            fixture,
            surfaceId,
            lighter,
            baseline,
            scratchDirectory,
            effectiveTimeout,
            onWordTimed
        );
        return fallback.Verdict switch
        {
            // The lighter mutation proved nothing; the exploding deletion is the only real
            // neutralization on offer, so report it rather than a mutation that quietly proves nothing.
            CounterfactualVerdict.Unobservable => primary,
            CounterfactualVerdict.Timeout => primary with
            {
                Delta =
                    primary.Delta + " (a lighter, children-only mutation was also tried and also did not terminate)",
            },
            _ => fallback with { Mutation = $"{fallback.Mutation}, in place of a deletion that did not terminate" },
        };
    }

    /// <summary>Whether an enum-sibling search should keep trying remaining siblings.</summary>
    public enum EnumSiblingSearchAction
    {
        Continue,
        Stop,
    }

    /// <summary>
    /// An enum rewrite always targets an attribute value the DTD already permits (a declared sibling),
    /// so it can never itself fail generic DTD validation -- a load failure reached this way is always
    /// <see cref="CounterfactualVerdict.RequiredByLoader"/>, never <see
    /// cref="CounterfactualVerdict.RequiredByDtd"/>.
    /// </summary>
    private static bool IsLoadFailureFromEnumRewrite(CounterfactualVerdict verdict) =>
        verdict == CounterfactualVerdict.RequiredByLoader;

    /// <summary>
    /// Folds one enum-sibling mutation result into the best found so far. <see
    /// cref="CounterfactualVerdict.Evidenced"/> (a word-level counter-example) always wins and stops
    /// the search; a load failure (see <see cref="IsLoadFailureFromEnumRewrite"/>) is kept only until
    /// something stronger turns up. Word and LoadFailure are different strengths of evidence (see
    /// <see cref="CounterexampleKind"/>), so stopping at whichever verdict a caller happens to reach
    /// first in declared sibling order would make the recorded strength depend on the DTD's
    /// alphabetical value ordering rather than on what the grammar actually shows.
    /// </summary>
    public static (CounterfactualResult? Best, EnumSiblingSearchAction Action) ConsiderEnumSiblingResult(
        CounterfactualResult? bestSoFar,
        CounterfactualResult candidate
    )
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (candidate.Verdict == CounterfactualVerdict.Evidenced)
            return (candidate, EnumSiblingSearchAction.Stop);
        if (IsLoadFailureFromEnumRewrite(candidate.Verdict) && bestSoFar is null)
            return (candidate, EnumSiblingSearchAction.Continue);
        return (bestSoFar, EnumSiblingSearchAction.Continue);
    }

    /// <summary>
    /// Tries every declared sibling of an enum surface, preferring a word-level
    /// <see cref="CounterfactualVerdict.Evidenced"/> result over a <see
    /// cref="CounterfactualVerdict.RequiredByLoader"/> one -- see <see cref="ConsiderEnumSiblingResult"/>.
    /// Returns null (never a <see cref="CounterfactualVerdict.Unobservable"/> result) when there are
    /// no siblings to enumerate -- not an enum surface, absent from the document, or with no declared
    /// sibling at all -- so <see cref="Evaluate"/> falls back to <see cref="GrammarMutator.Mutate"/>'s
    /// single-mutation path, which is the only neutralization available in that last case (a removal,
    /// not a rewrite).
    /// </summary>
    private static CounterfactualResult? EvaluateEveryEnumSibling(
        Fixture fixture,
        string surfaceId,
        SemanticInventory inventory,
        IReadOnlyList<string> baseline,
        string scratchDirectory,
        TimeSpan timeout,
        Action<string, long>? onWordTimed,
        XDocument grammar
    )
    {
        IReadOnlyList<GrammarMutator.EnumSiblingCandidate> candidates = GrammarMutator.MutateEnumAgainstEverySibling(
            grammar,
            surfaceId,
            inventory
        );
        if (candidates.Count == 0)
            return null;

        var tried = new List<string>();
        CounterfactualResult? bestSoFar = null;
        string? bestSibling = null;
        foreach (GrammarMutator.EnumSiblingCandidate candidate in candidates)
        {
            CounterfactualResult result = RunMutation(
                fixture,
                surfaceId,
                candidate.Mutation,
                baseline,
                scratchDirectory,
                timeout,
                onWordTimed
            );
            tried.Add($"\"{candidate.Sibling}\" ({result.Verdict})");

            (CounterfactualResult? updated, EnumSiblingSearchAction action) = ConsiderEnumSiblingResult(
                bestSoFar,
                result
            );
            if (!ReferenceEquals(updated, bestSoFar))
            {
                bestSoFar = updated;
                bestSibling = candidate.Sibling;
            }

            if (action == EnumSiblingSearchAction.Stop)
            {
                return bestSoFar! with
                {
                    Delta = $"sibling \"{bestSibling}\" of {candidates.Count} tried: {bestSoFar!.Delta}",
                };
            }
        }

        if (bestSoFar is not null)
        {
            return bestSoFar with
            {
                Delta =
                    $"sibling \"{bestSibling}\" of {candidates.Count} tried ({bestSoFar.Verdict}; no sibling produced "
                    + $"word-level evidence): {bestSoFar.Delta}",
            };
        }

        string siblingList = string.Join(", ", candidates.Select(c => $"\"{c.Sibling}\""));
        return new CounterfactualResult(
            surfaceId,
            fixture.Id,
            CounterfactualVerdict.Unobservable,
            $"tried {candidates.Count} declared sibling(s): {siblingList}",
            $"none produced a delta: {string.Join(", ", tried)}"
        );
    }

    /// <summary>
    /// Evidences an <c>isActive="no"</c> surface that <see cref="Evaluate"/> cannot: one whose only
    /// consumer is a fail-fast IDREF lookup, so activating it alone never dangles (nothing references
    /// it) and activating a live reference to it alone never loads (the baseline correctly still has
    /// the target inactive). Runs THREE mutants against the same baseline -- target alone, the
    /// referencing partner alone, and both together -- and only credits the target when all three of
    /// the docstring's necessity conditions hold: alone changes nothing, the partner alone does not
    /// already explain the joint delta, and jointly something does change. All three outcomes are
    /// folded into the returned result's <c>Delta</c> so the attribution is checkable, not asserted.
    /// </summary>
    public static CounterfactualResult EvaluateJointly(
        Fixture fixture,
        string surfaceId,
        SemanticInventory inventory,
        IReadOnlyList<string> baseline,
        string scratchDirectory,
        TimeSpan? timeout = null,
        Action<string, long>? onWordTimed = null
    )
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(baseline);

        GrammarMutator.JointPartner? partner = GrammarMutator.FindJointPartner(
            XDocument.Load(fixture.GrammarPath),
            surfaceId
        );
        if (partner is null)
        {
            return new CounterfactualResult(
                surfaceId,
                fixture.Id,
                CounterfactualVerdict.Unobservable,
                "none",
                "no independently-inactive referencing declaration exists to pair a joint mutation against"
            );
        }

        TimeSpan effectiveTimeout = timeout ?? DefaultTimeout;
        GrammarMutation? targetAlone = GrammarMutator.Mutate(XDocument.Load(fixture.GrammarPath), surfaceId, inventory);
        GrammarMutation? partnerAlone = GrammarMutator.MutatePartnerAlone(XDocument.Load(fixture.GrammarPath), partner);
        GrammarMutation? joint = GrammarMutator.MutateJointly(
            XDocument.Load(fixture.GrammarPath),
            surfaceId,
            partner,
            inventory
        );
        if (targetAlone is null || partnerAlone is null || joint is null)
        {
            return new CounterfactualResult(
                surfaceId,
                fixture.Id,
                CounterfactualVerdict.Unobservable,
                "none",
                "a joint partner was located but one of the three required mutations could not be built"
            );
        }

        CounterfactualResult resultA = RunMutation(
            fixture,
            surfaceId,
            targetAlone,
            baseline,
            scratchDirectory,
            effectiveTimeout,
            onWordTimed
        );
        CounterfactualResult resultB = RunMutation(
            fixture,
            surfaceId,
            partnerAlone,
            baseline,
            scratchDirectory,
            effectiveTimeout,
            onWordTimed
        );
        CounterfactualResult resultJoint = RunMutation(
            fixture,
            surfaceId,
            joint,
            baseline,
            scratchDirectory,
            effectiveTimeout,
            onWordTimed
        );

        string threeRun =
            $"three-run: target alone ({targetAlone.Detail}) -> {resultA.Verdict} ({resultA.Delta}); "
            + $"partner alone ({partnerAlone.Detail}) -> {resultB.Verdict} ({resultB.Delta}); "
            + $"both jointly ({joint.Detail}) -> {resultJoint.Verdict} ({resultJoint.Delta})";

        // Condition 1: the target alone must already be insufficient, or this is ordinary
        // single-surface evidence and reporting it as merely "joint" would UNDERSTATE it.
        if (resultA.Verdict is CounterfactualVerdict.Evidenced or CounterfactualVerdict.RequiredByLoader)
            return resultA;
        if (resultA.Verdict is not CounterfactualVerdict.Unobservable)
        {
            // Timeout, most likely. No clean "alone changes nothing" baseline to reason from, so
            // claiming joint attribution here would not be safe.
            return new CounterfactualResult(
                surfaceId,
                fixture.Id,
                resultA.Verdict,
                targetAlone.Detail,
                $"the target-alone run did not settle cleanly ({resultA.Delta}); {threeRun}"
            );
        }

        // Condition 3: the joint mutation must actually change something -- otherwise the pair proves
        // nothing either, honestly reported as still-unobservable rather than forced to a verdict.
        if (resultJoint.Verdict is not (CounterfactualVerdict.Evidenced or CounterfactualVerdict.RequiredByLoader))
        {
            return new CounterfactualResult(
                surfaceId,
                fixture.Id,
                CounterfactualVerdict.Unobservable,
                joint.Detail,
                $"joint activation produced no change either; {threeRun}"
            );
        }

        // Condition 2: the partner alone must not already reproduce the identical delta -- if it does,
        // the joint result is attributable to the partner, not to this surface, and must NOT evidence it.
        bool partnerAloneExplainsTheJointDelta =
            resultB.Verdict == resultJoint.Verdict && resultB.Delta == resultJoint.Delta;
        if (partnerAloneExplainsTheJointDelta)
        {
            return new CounterfactualResult(
                surfaceId,
                fixture.Id,
                CounterfactualVerdict.Unobservable,
                joint.Detail,
                $"the partner alone reproduces the identical delta, so it is not attributable to this surface; {threeRun}"
            );
        }

        // Carries resultJoint's ExampleWord/ExampleOutcome/CounterexampleKind/CounterexampleOutcome
        // forward rather than dropping them: resultJoint is already Evidenced or RequiredByLoader at
        // this point, so RunMutation already populated a real counter-example, and EvidencedJointly is a
        // strength label on that same counter-example, not a different, weaker one that has none.
        return resultJoint with
        {
            Verdict = CounterfactualVerdict.EvidencedJointly,
            Mutation = joint.Detail,
            Delta = threeRun,
        };
    }

    /// <summary>Applies one already-built mutation and diffs it against the baseline.</summary>
    private static CounterfactualResult RunMutation(
        Fixture fixture,
        string surfaceId,
        GrammarMutation mutation,
        IReadOnlyList<string> baseline,
        string scratchDirectory,
        TimeSpan timeout,
        Action<string, long>? onWordTimed
    )
    {
        string[] words = fixture.Words.Words.Select(word => word.Word).ToArray();

        Directory.CreateDirectory(scratchDirectory);
        string mutatedPath = Path.Combine(scratchDirectory, $"mutated-{Guid.NewGuid():N}.xml");
        // The loader resolves the DTD relative to the document, so the mutant has to sit beside a copy.
        string dtdSource = Path.Combine(Path.GetDirectoryName(fixture.GrammarPath)!, "HermitCrabInput.dtd");
        if (File.Exists(dtdSource))
            File.Copy(dtdSource, Path.Combine(scratchDirectory, "HermitCrabInput.dtd"), overwrite: true);
        try
        {
            IReadOnlyList<string> mutated;
            try
            {
                // Neutralizing the root element leaves a document that will not even serialize -- the
                // most extreme case of RequiredByDtd there is, since it fails before DTD validation
                // itself gets a document to validate.
                mutation.Mutated.Save(mutatedPath);
                mutated = OutcomesWithTimeout(mutatedPath, words, timeout, onWordTimed);
            }
            catch (TimeoutException)
            {
                // Not finishing in a wall-clock bound is timing-dependent, not a semantic delta.
                return new CounterfactualResult(
                    surfaceId,
                    fixture.Id,
                    CounterfactualVerdict.Timeout,
                    mutation.Detail,
                    $"the mutant did not terminate within {timeout.TotalSeconds:0}s"
                );
            }
            catch (Exception ex)
            {
                string loadFailure = $"{ex.GetType().Name}: {Summarize(ex.Message)}";
                return new CounterfactualResult(
                    surfaceId,
                    fixture.Id,
                    LoadFailureVerdict(mutation.Kind),
                    mutation.Detail,
                    loadFailure,
                    ExampleWord: words.Length != 0 ? words[0] : null,
                    ExampleOutcome: words.Length != 0 ? baseline[0] : null,
                    CounterexampleKind: CounterexampleKind.LoadFailure,
                    CounterexampleOutcome: loadFailure
                );
            }

            for (int i = 0; i < words.Length; i++)
            {
                if (baseline[i] != mutated[i])
                {
                    return new CounterfactualResult(
                        surfaceId,
                        fixture.Id,
                        CounterfactualVerdict.Evidenced,
                        mutation.Detail,
                        $"'{words[i]}': {baseline[i]} -> {mutated[i]}",
                        ExampleWord: words[i],
                        ExampleOutcome: baseline[i],
                        CounterexampleKind: CounterexampleKind.Word,
                        CounterexampleOutcome: mutated[i]
                    );
                }
            }

            return new CounterfactualResult(
                surfaceId,
                fixture.Id,
                CounterfactualVerdict.Unobservable,
                mutation.Detail,
                $"all {words.Length} word(s) unchanged"
            );
        }
        finally
        {
            try
            {
                if (File.Exists(mutatedPath))
                    File.Delete(mutatedPath);
            }
            catch (IOException)
            {
                // An abandoned timed-out worker task may still hold this file open; leaving it
                // behind in scratch is cheaper than blocking on a thread we deliberately gave up on.
            }
        }
    }

    /// <summary>
    /// Classifies a load failure by the neutralization <paramref name="mutationKind"/> that produced it,
    /// mechanically rather than by inspecting the exception -- <see
    /// cref="GrammarMutator.DeletedElements"/> and <see cref="GrammarMutator.EmptiedChildren"/> only ever
    /// remove or hollow out an element (a <c>dtd:element/*</c> surface), which can fail only by tripping
    /// generic DTD content-model validation before <c>XmlLanguageLoader</c> runs; every other kind
    /// (<see cref="GrammarMutator.RewroteAttribute"/>, <see cref="GrammarMutator.RemovedAttribute"/>,
    /// <see cref="GrammarMutator.ActivatedPartnerAlone"/>, <see cref="GrammarMutator.ActivatedJointly"/>)
    /// only ever touches a <c>dtd:enum/*</c> attribute already legal under the DTD, so a failure there
    /// can only come from HermitCrab's own loader.
    /// </summary>
    private static CounterfactualVerdict LoadFailureVerdict(string mutationKind) =>
        mutationKind is GrammarMutator.DeletedElements or GrammarMutator.EmptiedChildren
            ? CounterfactualVerdict.RequiredByDtd
            : CounterfactualVerdict.RequiredByLoader;

    private static string Summarize(string message)
    {
        string single = message.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return single.Length <= 120 ? single : single[..120] + "...";
    }
}
