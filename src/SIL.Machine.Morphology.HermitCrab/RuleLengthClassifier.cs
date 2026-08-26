using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// What un-applying a morphological rule does to the length of the word, decided statically from the
    /// rule's own declaration (hermitcrab-forest-memo-plan.md Stage 1).
    /// </summary>
    internal enum UnapplicationLengthEffect
    {
        /// <summary>
        /// Every un-application strictly shortens the word. Such a rule cannot drive an infinite regress --
        /// shape length is already a decreasing measure -- so it does not need to be counted in
        /// <see cref="AnalysisStateKey"/> to guarantee termination.
        /// </summary>
        Shrinking,

        /// <summary>
        /// Some un-application leaves the word the same length or longer (a zero morpheme, or a rule that
        /// deletes material on synthesis and therefore restores it on analysis). These are exactly the
        /// rules that can loop, so they stay in the key.
        /// </summary>
        NonShrinking,

        /// <summary>
        /// Not decidable by this classifier. Treated exactly like <see cref="NonShrinking"/> by the key --
        /// kept distinct only so a grammar census can tell "this grammar has zero morphemes" apart from
        /// "this grammar has constructs we cannot analyse."
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Classifies each morphological rule by <see cref="UnapplicationLengthEffect"/> so that
    /// <see cref="AnalysisStateKey"/> can drop the per-rule un-application counts that do no work.
    ///
    /// The direction is the thing to keep straight: on the ANALYSIS side rules are un-applied, so an
    /// affix rule that INSERTS material on synthesis REMOVES it on analysis (shrinking), while a rule that
    /// DELETES material on synthesis RESTORES it on analysis (growing). Getting this backwards would
    /// silently drop the rules that actually need counting.
    ///
    /// Conservative by construction: <see cref="UnapplicationLengthEffect.Unknown"/> is the safe answer,
    /// because retaining a rule in the key is the status quo and always sound. Only a rule proved to
    /// shorten the word on every possible un-application is reported as
    /// <see cref="UnapplicationLengthEffect.Shrinking"/>.
    /// </summary>
    internal static class RuleLengthClassifier
    {
        public static UnapplicationLengthEffect Classify(IMorphologicalRule rule)
        {
            switch (rule)
            {
                case AffixProcessRule affixRule:
                    return ClassifyAllomorphs(affixRule.Allomorphs);
                case RealizationalAffixProcessRule realizationalRule:
                    return ClassifyAllomorphs(realizationalRule.Allomorphs);

                // A compounding rule's un-application splits one word into a head and a non-head. The head
                // shortens, but no material is destroyed, and the interesting bound is MaxStemCount rather
                // than length. Word.NonHeadCount is already a key component in its own right, so there is
                // nothing to gain by reasoning harder here.
                case CompoundingRule _:
                    return UnapplicationLengthEffect.Unknown;

                default:
                    return UnapplicationLengthEffect.Unknown;
            }
        }

        /// <summary>
        /// Which rules must stay in <see cref="AnalysisStateKey"/>'s count multiset, built once per
        /// grammar rather than once per key: key construction is on the hottest path in the engine, so
        /// this must not become a per-key filter. Absent from the map means "retain" -- a rule reached
        /// through a path this walk does not cover is retained, never dropped.
        /// </summary>
        public static IReadOnlyDictionary<IMorphologicalRule, bool> BuildRetainInKeyMap(Language language)
        {
            var map = new Dictionary<IMorphologicalRule, bool>();
            foreach (Stratum stratum in language.Strata)
            {
                foreach (IMorphologicalRule rule in stratum.MorphologicalRules)
                    map[rule] = Classify(rule) != UnapplicationLengthEffect.Shrinking;
            }
            return map;
        }

        private static UnapplicationLengthEffect ClassifyAllomorphs(IEnumerable<AffixProcessAllomorph> allomorphs)
        {
            bool any = false;
            var effect = UnapplicationLengthEffect.Shrinking;
            foreach (AffixProcessAllomorph allomorph in allomorphs)
            {
                any = true;
                UnapplicationLengthEffect allomorphEffect = ClassifyAllomorph(allomorph);
                if (allomorphEffect == UnapplicationLengthEffect.Unknown)
                    return UnapplicationLengthEffect.Unknown;
                if (allomorphEffect == UnapplicationLengthEffect.NonShrinking)
                    effect = UnapplicationLengthEffect.NonShrinking;
            }
            // A rule with no allomorphs cannot be reasoned about; it also cannot un-apply, but say Unknown
            // rather than assert that.
            return any ? effect : UnapplicationLengthEffect.Unknown;
        }

        /// <summary>
        /// One allomorph shrinks on un-application iff the analysis output is strictly shorter than the
        /// analysis input. <see cref="AnalysisMorphologicalTransform"/> is the authority on what that
        /// output is: it walks the allomorph's Lhs parts and, for each, either copies the span captured by
        /// the corresponding Rhs <see cref="CopyFromInput"/>/<see cref="ModifyFromInput"/> (length
        /// preserved) or calls <c>Untruncate</c> to regenerate the part from its pattern (length ADDED --
        /// this is synthesis-side deletion showing up as analysis-side growth). Meanwhile the Rhs's
        /// <see cref="InsertSegments"/>/<see cref="InsertSimpleContext"/> material is matched away by the
        /// analysis pattern and does not reach the output (length removed).
        ///
        /// So: shrinking iff every Lhs part is captured, at least one segment is inserted, and no part is
        /// copied more than once.
        /// </summary>
        private static UnapplicationLengthEffect ClassifyAllomorph(AffixProcessAllomorph allomorph)
        {
            int inserted = 0;
            var capturedParts = new HashSet<string>();
            foreach (MorphologicalOutputAction action in allomorph.Rhs)
            {
                switch (action)
                {
                    case InsertSegments insertSegments:
                        inserted += SegmentCount(insertSegments.Segments.Shape);
                        break;
                    case InsertSimpleContext _:
                        inserted += 1;
                        break;
                    case CopyFromInput copyFromInput:
                        // Reduplication: the same part copied twice. AnalysisMorphologicalTransform emits
                        // only ONE instance, so this does in fact shrink -- but the un-application is
                        // nondeterministic in ways this walk does not model, so take the safe answer.
                        if (!capturedParts.Add(copyFromInput.PartName))
                            return UnapplicationLengthEffect.Unknown;
                        break;
                    case ModifyFromInput modifyFromInput:
                        if (!capturedParts.Add(modifyFromInput.PartName))
                            return UnapplicationLengthEffect.Unknown;
                        break;
                    default:
                        return UnapplicationLengthEffect.Unknown;
                }
            }

            // An Lhs part with no corresponding Rhs copy is truncated on synthesis and untruncated on
            // analysis. Untruncate adds a node per segment Constraint in the part's pattern, so the
            // un-applied word GROWS. (A part whose pattern contains no segment constraints would add
            // nothing, but proving that is a refinement this classifier does not need: report
            // NonShrinking, which is safe.)
            foreach (Pattern<Word, ShapeNode> part in allomorph.Lhs)
            {
                if (!capturedParts.Contains(part.Name))
                    return UnapplicationLengthEffect.NonShrinking;
            }

            // No material inserted on synthesis means nothing removed on analysis: a zero morpheme. These
            // are exactly the rules that can produce the N -> V -> N cycle, so they must stay in the key.
            return inserted > 0 ? UnapplicationLengthEffect.Shrinking : UnapplicationLengthEffect.NonShrinking;
        }

        private static int SegmentCount(Shape shape)
        {
            int count = 0;
            foreach (ShapeNode node in shape)
            {
                if (node.Annotation.Type() == HCFeatureSystem.Segment)
                    count++;
            }
            return count;
        }
    }
}
