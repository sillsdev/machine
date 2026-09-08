using System.Threading;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    /// <summary>
    /// How an analysis (un-application) rule folds a rule's required syntactic feature structure into the
    /// syntactic feature structure of the word it produces. Research toggle for sillsdev/machine PR #494.
    /// </summary>
    internal enum AnalysisSyntacticFeatureMergeMode
    {
        /// <summary>master behaviour: Add (per-feature value union; keeps everything already present).</summary>
        Add,

        /// <summary>PR #494: PriorityUnion (required overrides; features not in required are kept).</summary>
        PriorityUnion,

        /// <summary>
        /// Exact inverse of synthesis. Synthesis produces PU(stem AND required, out), so the constraint on the
        /// stem is (input with out's feature paths removed) unified with required, and the un-application is
        /// only possible when the input unifies with PU(required, out). Never clears the feature structure.
        /// </summary>
        Exact,
    }

    internal static class AnalysisSyntacticFeatureMerge
    {
        /// <summary>
        /// Default PriorityUnion (PR #494). Override per process with env HC_ANALYSIS_FS_MERGE=Add|PriorityUnion|Exact
        /// so the whole test suite or a benchmark can run under one mode without a code change.
        /// </summary>
        public static AnalysisSyntacticFeatureMergeMode Mode = ReadModeFromEnvironment();

        private static AnalysisSyntacticFeatureMergeMode ReadModeFromEnvironment()
        {
            string value = System.Environment.GetEnvironmentVariable("HC_ANALYSIS_FS_MERGE");
            if (
                !string.IsNullOrEmpty(value)
                && System.Enum.TryParse(value, true, out AnalysisSyntacticFeatureMergeMode mode)
            )
            {
                return mode;
            }
            return AnalysisSyntacticFeatureMergeMode.PriorityUnion;
        }

        // Deterministic work counters (research only). Interlocked so parallel parsing stays countable.
        public static long CheckCalls;
        public static long CheckRejects;
        public static long Merges;
        public static long ExactUnifyFailures;
        public static long AnalysisAffixApplyCalls;
        public static long AnalysisUnapplied;
        public static long LexicalLookupCandidates;
        public static long SynthesisAffixApplyCalls;

        public static void ResetCounters()
        {
            CheckCalls = CheckRejects = Merges = ExactUnifyFailures = 0;
            AnalysisAffixApplyCalls = AnalysisUnapplied = LexicalLookupCandidates = SynthesisAffixApplyCalls = 0;
        }

        /// <summary>PU(required, out): the most general syntactic FS synthesis can produce from this rule.</summary>
        public static FeatureStruct BuildCheckFeatureStruct(FeatureStruct required, FeatureStruct outFs)
        {
            FeatureStruct fs = required.Clone();
            fs.PriorityUnion(outFs);
            fs.Freeze();
            return fs;
        }

        /// <summary>Can this rule have produced <paramref name="input"/>?</summary>
        public static bool CanUnapply(FeatureStruct outFs, FeatureStruct checkFs, FeatureStruct input)
        {
            Interlocked.Increment(ref CheckCalls);
            bool ok =
                Mode == AnalysisSyntacticFeatureMergeMode.Exact
                    ? checkFs.IsUnifiable(input)
                    : outFs.IsUnifiable(input);
            if (!ok)
                Interlocked.Increment(ref CheckRejects);
            return ok;
        }

        /// <summary>Fold the rule's required FS into the un-applied word's syntactic FS.</summary>
        public static void MergeRequired(Word outWord, FeatureStruct required, FeatureStruct outFs)
        {
            Interlocked.Increment(ref Merges);
            switch (Mode)
            {
                case AnalysisSyntacticFeatureMergeMode.Add:
                    if (!required.IsEmpty)
                        outWord.SyntacticFeatureStruct.Add(required);
                    else if (outFs.IsEmpty)
                        outWord.SyntacticFeatureStruct.Clear();
                    break;
                case AnalysisSyntacticFeatureMergeMode.PriorityUnion:
                    if (!required.IsEmpty)
                        outWord.SyntacticFeatureStruct.PriorityUnion(required);
                    else if (outFs.IsEmpty)
                        outWord.SyntacticFeatureStruct.Clear();
                    break;
                case AnalysisSyntacticFeatureMergeMode.Exact:
                    FeatureStruct fs = outWord.SyntacticFeatureStruct;
                    RemovePaths(fs, outFs);
                    if (!required.IsEmpty)
                    {
                        if (fs.Unify(required, out FeatureStruct unified))
                        {
                            outWord.SyntacticFeatureStruct = unified;
                        }
                        else
                        {
                            // Cannot happen after CanUnapply succeeded; fall back to the PR #494 behaviour.
                            Interlocked.Increment(ref ExactUnifyFailures);
                            fs.PriorityUnion(required);
                        }
                    }
                    break;
            }
        }

        /// <summary>Remove from <paramref name="fs"/> every leaf feature path that <paramref name="paths"/> defines.</summary>
        private static void RemovePaths(FeatureStruct fs, FeatureStruct paths)
        {
            foreach (Feature feature in paths.Features)
            {
                if (!fs.TryGetValue(feature, out FeatureValue thisValue))
                    continue;
                if (paths.GetValue(feature) is FeatureStruct pathFs && thisValue is FeatureStruct thisFs)
                {
                    RemovePaths(thisFs, pathFs);
                    if (thisFs.IsEmpty)
                        fs.RemoveValue(feature);
                }
                else
                {
                    fs.RemoveValue(feature);
                }
            }
        }
    }
}
