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
    /// (<see cref="Morphology.HermitCrab.MorphologicalRules.AffixProcessRule.MaxApplicationCount"/>,
    /// <see cref="Morpher.DeletionReapplications"/>), never estimated -- so it can prune a candidate only
    /// when NO combination of rules in the grammar could ever produce something that long, regardless of
    /// which specific root or derivation path is under consideration. Returns null (meaning "no admissible
    /// bound, gate off") the moment any rule's shape falls outside what this class knows how to measure
    /// exactly (quantifiers/groups/alternations in a phonological Lhs/Rhs, an insertion-type rewrite
    /// subrule -- epenthesis/expansion, whose unapplication marks surface segments optional instead of
    /// removing them, so the running shape length stops bounding the underlying length; see LT-22613 --
    /// or a compounding rule present at all, since compounding combines multiple full root lengths rather
    /// than adding a bounded affix) -- per the plan's own rule: skipping only costs pruning opportunity,
    /// an admissible bound must never be guessed.
    /// </summary>
    public static class GrammarAnalyzer
    {
        /// <summary>
        /// The longest possible underlying form (in real segments) any analysis candidate could validly
        /// represent: the longest root allomorph in the lexicon, plus every affix/realizational rule's own
        /// maximum possible net insertion (its allomorphs' <see cref="InsertSegments"/>/
        /// <see cref="InsertSimpleContext"/> actions, summed and multiplied by
        /// <see cref="Morphology.HermitCrab.MorphologicalRules.AffixProcessRule.MaxApplicationCount"/>),
        /// plus every phonological deletion-type subrule's maximum possible net restoration. Null if any
        /// rule in the grammar can't be measured this way (see class remarks).
        /// </summary>
        /// <remarks>
        /// The phonological term is compounding, not additive: <c>AnalysisRewriteRule</c>'s Deletion
        /// reapply loop runs <see cref="Morpher.DeletionReapplications"/> + 1 passes, and each pass is a
        /// <c>SimultaneousPhonologicalPatternRule</c> sweep that can restore EVERY non-overlapping match
        /// site in the current shape at once, not just one -- a real case (<c>RewriteRuleTests
        /// .MultipleDeletionRules</c>: an 8-segment root deletes two independent "ii" clusters down to a
        /// 4-segment surface form in one pass) needs more than "count of subrules" restored segments per
        /// pass. Bounding the number of sites by the current running length (itself already an
        /// over-approximation of the true pre-phonology length at this point) keeps this sound: real growth
        /// can never exceed <c>runningLength * subruleDelta</c> per pass, since a simultaneous sweep cannot
        /// match more sites than there are segments to match against.
        /// </remarks>
        public static int? ComputeMaxAnalysisLength(Language language, int deletionReapplications)
        {
            int bound = 0;
            foreach (Stratum stratum in language.Strata)
            {
                if (stratum.MorphologicalRules.OfType<CompoundingRule>().Any())
                    return null;

                int longestRoot = stratum.Entries.SelectMany(e => e.Allomorphs).Select(SegmentCount).DefaultIfEmpty(0).Max();
                bound += longestRoot;

                foreach (AffixProcessRule rule in stratum.MorphologicalRules.OfType<AffixProcessRule>())
                    bound += MaxAllomorphInsertion(rule.Allomorphs) * rule.MaxApplicationCount;

                foreach (
                    RealizationalAffixProcessRule rule in stratum.MorphologicalRules.OfType<RealizationalAffixProcessRule>()
                )
                    bound += MaxAllomorphInsertion(rule.Allomorphs);

                int phonoGrowthRate = 0;
                foreach (RewriteRule rule in stratum.PhonologicalRules.OfType<RewriteRule>())
                {
                    if (!TryGetFlatSegmentCount(rule.Lhs, out int lhsCount))
                        return null;
                    foreach (RewriteSubrule sr in rule.Subrules)
                    {
                        if (!TryGetFlatSegmentCount(sr.Rhs, out int rhsCount))
                            return null;
                        // Insertion-type subrule (epenthesis when Lhs is empty, expansion otherwise):
                        // no admissible bound (LT-22613). Unlike every other unapplication this class
                        // reasons about, insertion unapplication does not SHRINK the candidate -- it
                        // marks the possibly-rule-inserted surface segments Optional and leaves them in
                        // the shape (EpenthesisAnalysisRewriteRuleSpec.Unapply /
                        // NarrowAnalysisRewriteRuleSpec.Unapply), and lexical lookup later matches with
                        // those segments skipped. AnalysisStratumRule's gate measures the candidate with
                        // Shape.SegmentCount, which still counts them, so a surface form that legally
                        // outgrew the longest root via epenthesis (e.g. "buibui" from root "b+ubu")
                        // would be pruned as unreachable on the default, non-tracing path while the
                        // traced path -- which bypasses the gate -- parses it correctly. Per this
                        // class's ground rule, that means return null (gate off), never guess.
                        if (lhsCount < rhsCount)
                            return null;
                        if (lhsCount > rhsCount)
                            phonoGrowthRate += lhsCount - rhsCount;
                    }
                }
                for (int pass = 0; pass < deletionReapplications + 1 && phonoGrowthRate > 0; pass++)
                    bound += bound * phonoGrowthRate;
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
