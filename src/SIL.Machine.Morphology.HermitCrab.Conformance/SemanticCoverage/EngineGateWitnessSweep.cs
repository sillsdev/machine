#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Morphology.HermitCrab.Conformance.SemanticCoverage;

/// <summary>
/// Runs every non-pathological, non-crash fixture's words through the reference engine with tracing
/// on and records every non-<see cref="FailureReason.None"/> value observed ANYWHERE in the resulting
/// trace tree, tagged to the fixture and word that produced it -- the evidence behind
/// <see cref="EngineGateInventoryLedger"/>'s <c>triggered_by_fixtures</c>/<c>triggering_words</c>/
/// <c>status</c> columns.
///
/// <b>This is a materially weaker claim than <see cref="TraceRuleAttributor"/>/<see cref="FailureRuleAttributor"/>'s
/// per-rule attribution, deliberately.</b> Those two are careful to attribute a reason only to a rule
/// that provably OWNS the failing/blocked candidate -- <see cref="FailureRuleAttributor"/>'s own doc
/// comment records that a reason like <see cref="FailureReason.RequiredMprFeatures"/> fires routinely
/// for a rule merely TRIED against a candidate it has nothing to do with, in a linear/unordered
/// stratum search, and there is no <see cref="FailureReason"/> value that reliably tells the two
/// apart. This sweep does not attempt to: it answers only "did this value appear anywhere while
/// parsing this word", which is the right (and only mechanically available) question for a gate
/// inventory keyed to the enum itself rather than to a specific rule instance. See
/// <see cref="EngineGateInventoryLedger"/>'s own doc comment for why a <c>Witnessed</c> gate here is a
/// weaker claim than a witnessed dataflow-obligation cell.
/// </summary>
public static class EngineGateWitnessSweep
{
    public sealed record Witness(string Gate, string FixtureId, string Word);

    public static IReadOnlyList<Witness> Sweep(string repositoryRoot, Action<string, int>? onFixtureStarted = null)
    {
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        var witnesses = new List<Witness>();

        foreach (Fixture fixture in Fixture.DiscoverAll(Path.Combine(repositoryRoot, "conformance")))
        {
            // Mirrors CounterfactualLedger.Sweep's own exclusion: a pathological (budgeted) fixture
            // exists precisely because some word may never terminate, and an expect_crash fixture's
            // ground truth is that the engine throws before ever returning a traceable result -- neither
            // has a trustworthy per-word trace tree to walk.
            if (fixture.Words.BudgetMs is not null || fixture.Words.ExpectCrash)
                continue;

            onFixtureStarted?.Invoke(fixture.Id, fixture.Words.Words.Count);

            Language language;
            try
            {
                language = XmlLanguageLoader.Load(fixture.GrammarPath);
            }
            catch
            {
                continue;
            }

            var traceManager = new TraceManager { IsTracing = true };
            var morpher = new Morpher(traceManager, language);

            foreach (WordEntry word in fixture.Words.Words)
            {
                bool guessRoot = word.Parses.Any(p => p.Guess);
                object? trace;
                try
                {
                    morpher.ParseWord(word.Word, out trace, guessRoot).ToList();
                }
                catch
                {
                    // InvalidShapeException (skip words) and any other engine exception leave no trace
                    // worth walking; a gate cannot be witnessed by a word the engine never traced.
                    continue;
                }

                foreach (string gate in ObservedGates(trace))
                    witnesses.Add(new Witness(gate, fixture.Id, word.Word));
            }
        }

        return witnesses;
    }

    private static IReadOnlyList<string> ObservedGates(object? trace)
    {
        var gates = new HashSet<string>(StringComparer.Ordinal);
        if (trace is Trace root)
            Walk(root);
        return gates.ToArray();

        void Walk(Trace node)
        {
            if (node.FailureReason != FailureReason.None)
                gates.Add(node.FailureReason.ToString());
            foreach (Trace child in node.Children)
                Walk(child);
        }
    }
}
