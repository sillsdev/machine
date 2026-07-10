using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Morphology.HermitCrab.PhonologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Computes a grammar-wide, word-independent bound on how long an underlying (analysis) form can
    /// validly be, relative to the lexicon and rule set actually declared (parse-optimization.md Phase 4's
    /// "Gate B" -- Gate A, a mirror-image synthesis-side bound, was attempted and reverted; see the note
    /// in <see cref="Morpher.SynthesizeAnalysis"/>). The bound is a deliberately loose over-approximation
    /// -- summed across every rule's own already-declared reapplication limit
    /// (<see cref="Morphology.HermitCrab.MorphologicalRules.AffixProcessRule.MaxApplicationCount"/>),
    /// never estimated -- so it can prune a candidate only when NO combination of rules in the grammar
    /// could ever produce something that long, regardless of which specific root or derivation path is
    /// under consideration. Returns null (meaning "no admissible bound, gate off") the moment any rule's
    /// shape falls outside what this class knows how to measure exactly (quantifiers/groups/alternations
    /// in a phonological Lhs/Rhs, any phonological rewrite subrule whose Lhs and Rhs segment counts differ
    /// -- see LT-22613, and the remarks on <see cref="TryGetFlatSegmentCount"/>'s caller below -- or a
    /// compounding rule present at all, since compounding combines multiple full root lengths rather than
    /// adding a bounded affix) -- per the plan's own rule: skipping only costs pruning opportunity, an
    /// admissible bound must never be guessed.
    /// </summary>
    public static class GrammarAnalyzer
    {
        /// <summary>
        /// The longest possible underlying form (in real segments) any analysis candidate could validly
        /// represent: the longest root allomorph in the lexicon, plus every affix/realizational rule's own
        /// maximum possible net insertion (its allomorphs' <see cref="InsertSegments"/>/
        /// <see cref="InsertSimpleContext"/> actions, summed and multiplied by
        /// <see cref="Morphology.HermitCrab.MorphologicalRules.AffixProcessRule.MaxApplicationCount"/>).
        /// Null if any rule in the grammar can't be measured this way (see class remarks) -- in
        /// particular, every phonological rewrite subrule in the grammar must leave Lhs and Rhs segment
        /// counts equal (a pure feature-changing rule, handled analysis-side by
        /// <c>FeatureAnalysisRewriteRuleSpec</c>, which mutates matched segments in place and never
        /// changes the shape's length). Any subrule where they differ -- epenthesis/expansion (Rhs longer)
        /// or deletion/coalescence (Lhs longer) alike -- is unapplied analysis-side by
        /// <c>EpenthesisAnalysisRewriteRuleSpec</c>/<c>NarrowAnalysisRewriteRuleSpec</c>, both of which
        /// grow the candidate shape by inserting <c>Lhs.Count</c> new (Optional, not Deleted) nodes per
        /// match site regardless of <c>Rhs.Count</c>, while leaving the matched Rhs-shaped region in place
        /// (also not Deleted). <see cref="HermitCrabExtensions.SegmentCount"/> counts every non-Deleted
        /// segment, so real per-site growth is always exactly <c>Lhs.Count</c> -- never the naively
        /// expected <c>Lhs.Count - Rhs.Count</c>, which undercounts by <c>Rhs.Count</c> whenever
        /// <c>Rhs.Count &gt; 0</c> (LT-22613: first found via epenthesis pruning a valid analysis as
        /// "too long" on the default non-tracing path while the tracing path, which bypasses this gate,
        /// parsed it correctly; the same undercount also applies to ordinary deletion/coalescence rules,
        /// which is why the bail-out is on any count mismatch, not just Rhs longer than Lhs).
        /// </summary>
        public static int? ComputeMaxAnalysisLength(Language language)
        {
            int bound = 0;
            foreach (Stratum stratum in language.Strata)
            {
                if (stratum.MorphologicalRules.OfType<CompoundingRule>().Any())
                    return null;

                int longestRoot = stratum
                    .Entries.SelectMany(e => e.Allomorphs)
                    .Select(SegmentCount)
                    .DefaultIfEmpty(0)
                    .Max();
                bound += longestRoot;

                foreach (AffixProcessRule rule in stratum.MorphologicalRules.OfType<AffixProcessRule>())
                    bound += MaxAllomorphInsertion(rule.Allomorphs) * rule.MaxApplicationCount;

                foreach (
                    RealizationalAffixProcessRule rule in stratum.MorphologicalRules.OfType<RealizationalAffixProcessRule>()
                )
                    bound += MaxAllomorphInsertion(rule.Allomorphs);

                foreach (RewriteRule rule in stratum.PhonologicalRules.OfType<RewriteRule>())
                {
                    if (!TryGetFlatSegmentCount(rule.Lhs, out int lhsCount))
                        return null;
                    foreach (RewriteSubrule sr in rule.Subrules)
                    {
                        if (!TryGetFlatSegmentCount(sr.Rhs, out int rhsCount))
                            return null;
                        // Any subrule where Lhs and Rhs segment counts differ: no admissible bound
                        // (LT-22613; see this method's doc comment for the full mechanism). Only a
                        // count-preserving (pure feature-changing) subrule is safe to ignore here.
                        if (lhsCount != rhsCount)
                            return null;
                    }
                }
            }
            return bound;
        }

        /// <summary>
        /// parse-optimization.md Phase 5's edge-stripper qualification: true only if every affix rule in
        /// the grammar is a pure "copy a contiguous span of the input, optionally with material inserted
        /// only before/after it" transform, and no stratum has a <see cref="CompoundingRule"/> or a
        /// <see cref="MetathesisRule"/>. This is the soundness precondition for
        /// <see cref="Morpher.EnableLexicalGating"/>: <see cref="RootAllomorphTrie.ContainsRootAnywhere"/>
        /// assumes a root that exists in the lexicon must still appear as an intact contiguous window in
        /// any not-yet-fully-analyzed candidate. Reduplication (the same input span copied more than once)
        /// and infixation (material inserted BETWEEN two copied spans, splitting one span's material from
        /// another's) both break that assumption -- the true root window would be split or duplicated, so
        /// a real root could be invisible to a contiguous-window search. Compounding combines multiple
        /// independent root windows, and metathesis physically reorders segments -- both are also outside
        /// what a contiguous-window search over the ORIGINAL lexicon strings can safely reason about.
        /// This is a single whole-language verdict, not per-stratum: simpler and strictly safer than the
        /// per-stratum granularity the plan sketches (a grammar with one unqualified stratum disables the
        /// gate everywhere rather than only where it's actually unsafe).
        /// </summary>
        public static bool IsEdgeStripperQualified(Language language)
        {
            foreach (Stratum stratum in language.Strata)
            {
                if (stratum.MorphologicalRules.OfType<CompoundingRule>().Any())
                    return false;
                if (stratum.PhonologicalRules.OfType<MetathesisRule>().Any())
                    return false;

                foreach (AffixProcessRule rule in stratum.MorphologicalRules.OfType<AffixProcessRule>())
                {
                    if (rule.Allomorphs.Any(a => !IsEdgeStripperAllomorph(a)))
                        return false;
                }
                foreach (
                    RealizationalAffixProcessRule rule in stratum.MorphologicalRules.OfType<RealizationalAffixProcessRule>()
                )
                {
                    if (rule.Allomorphs.Any(a => !IsEdgeStripperAllomorph(a)))
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// An allomorph qualifies if its Rhs, scanned in order, looks like
        /// <c>[insert]* [copy]+ [insert]*</c> with every copied part name appearing at most once: all
        /// copied-from-input material forms one contiguous block (no insertion sandwiched between two
        /// copy actions -- that would be infixation, splitting the input material apart), and no part is
        /// copied twice (that would be reduplication).
        /// </summary>
        private static bool IsEdgeStripperAllomorph(AffixProcessAllomorph allomorph)
        {
            var seenParts = new HashSet<string>();
            bool sawCopy = false;
            bool sawInsertAfterCopy = false;
            foreach (MorphologicalOutputAction action in allomorph.Rhs)
            {
                switch (action)
                {
                    case CopyFromInput copyFromInput:
                        if (sawInsertAfterCopy || !seenParts.Add(copyFromInput.PartName))
                            return false;
                        sawCopy = true;
                        break;
                    case ModifyFromInput modifyFromInput:
                        if (sawInsertAfterCopy || !seenParts.Add(modifyFromInput.PartName))
                            return false;
                        sawCopy = true;
                        break;
                    case InsertSegments _:
                    case InsertSimpleContext _:
                        if (sawCopy)
                            sawInsertAfterCopy = true;
                        break;
                }
            }
            return true;
        }

        private static int SegmentCount(RootAllomorph allomorph) => allomorph.Segments.Shape.SegmentCount();

        private static int MaxAllomorphInsertion(IEnumerable<AffixProcessAllomorph> allomorphs)
        {
            int max = 0;
            foreach (AffixProcessAllomorph allo in allomorphs)
            {
                int insertion = 0;
                foreach (MorphologicalOutputAction action in allo.Rhs)
                {
                    switch (action)
                    {
                        case InsertSegments insertSegments:
                            insertion += insertSegments.Segments.Shape.SegmentCount();
                            break;
                        case InsertSimpleContext _:
                            insertion += 1;
                            break;
                        // CopyFromInput/ModifyFromInput carry forward material already matched from the
                        // input (the root or a nested part) -- already counted via the root/allomorph
                        // length elsewhere, so they contribute 0 NEW segments here.
                    }
                }
                if (insertion > max)
                    max = insertion;
            }
            return max;
        }

        private static bool TryGetFlatSegmentCount(Pattern<Word, int> pattern, out int count)
        {
            count = pattern.Children.Count;
            return pattern.Children.All(c => c is Constraint<Word, int> ctr && ctr.Type() == HCFeatureSystem.Segment);
        }
    }
}
