using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// How <see cref="AnalysisAffixTemplateRule"/> uses <see cref="RequiredSlotMaterial"/>.
    /// </summary>
    internal enum RequiredSlotLookaheadMode
    {
        /// <summary>The look-ahead is not evaluated.</summary>
        Off,

        /// <summary>The look-ahead is evaluated and reported to Morpher.RequiredSlotShadow, never pruning.</summary>
        Shadow,

        /// <summary>The look-ahead prunes template partials.</summary>
        On,
    }

    /// <summary>
    /// A necessary condition for a required affix template slot to unapply later in a template walk: some
    /// allomorph of some rule in the slot must have every inserted segment unify with a segment of the shape.
    /// </summary>
    /// <remarks>
    /// The tables are built once from the template and depend only on the grammar.
    ///
    /// Soundness. The analysis pattern of an allomorph holds one top-level constraint per inserted segment
    /// (<see cref="InsertSegments.GenerateAnalysisLhs"/>, <see cref="InsertSimpleContext.GenerateAnalysisLhs"/>),
    /// and the rule can only unapply if each constraint unifies with a distinct segment node of the shape.
    /// A rule is pure when every right-hand side action copies an input part or inserts material, and every
    /// left-hand part is copied by a <see cref="CopyFromInput"/> that must capture a segment, or has no
    /// segment to restore. Then <see cref="AnalysisMorphologicalTransform.GenerateShape"/> only copies input
    /// nodes and never untruncates a segment. Unification can only narrow the feature structure of a copied
    /// node, so a constraint that unifies with no node of the shape unifies with no node of the shape after
    /// any number of pure unapplications. Inside one template walk only the rules of that template's slots
    /// run, and the stratum's phonological rules ran before the walk. The test for slot j is therefore used
    /// only when every rule in the slots still to be walked after j is pure. Optional nodes count as present
    /// because the matcher can match them. Anything that cannot be summarised (a null affix, a constraint with
    /// variables, an unknown rule type) leaves the slot unprunable.
    /// </remarks>
    internal class RequiredSlotMaterial
    {
        private readonly SlotSummary[] _slots;

        public RequiredSlotMaterial(AffixTemplate template)
        {
            _slots = template.Slots.Select(Summarize).ToArray();
        }

        /// <summary>
        /// Whether every rule of the slot is pure, so that unapplying the slot leaves the segments needed by
        /// slots before it no easier to find.
        /// </summary>
        public bool IsPure(int slot)
        {
            return _slots[slot].IsPure;
        }

        /// <summary>
        /// True if no allomorph of the slot can unapply to a word with this shape, provided that every slot
        /// walked between this word and the slot is pure.
        /// </summary>
        /// <param name="slot">The slot to test.</param>
        /// <param name="shape">The shape of the partial.</param>
        /// <param name="unifications">Incremented by the feature structure unifications performed.</param>
        public bool CannotUnapply(int slot, Shape shape, ref long unifications)
        {
            SlotSummary summary = _slots[slot];
            if (summary.MayAlwaysUnapply)
                return false;

            // The constraints of a slot repeat across its allomorphs, so each distinct one is looked up once.
            ulong known = 0;
            ulong present = 0;
            foreach (int[] requirements in summary.Allomorphs)
            {
                bool all = true;
                foreach (int index in requirements)
                {
                    bool found;
                    ulong bit = index < 64 ? 1UL << index : 0;
                    if ((known & bit) != 0)
                    {
                        found = (present & bit) != 0;
                    }
                    else
                    {
                        found = IsPresent(summary.Distinct[index], shape, ref unifications);
                        known |= bit;
                        if (found)
                            present |= bit;
                    }
                    if (!found)
                    {
                        all = false;
                        break;
                    }
                }
                if (all)
                    return false;
            }
            return true;
        }

        private static bool IsPresent(FeatureStruct required, Shape shape, ref long unifications)
        {
            foreach (ShapeNode node in shape)
            {
                if (node.Type() != HCFeatureSystem.Segment)
                    continue;

                unifications++;
                if (node.Annotation.FeatureStruct.IsUnifiable(required, false, null))
                    return true;
            }
            return false;
        }

        private static SlotSummary Summarize(AffixTemplateSlot slot)
        {
            var summary = new SlotSummary();
            var allomorphs = new List<FeatureStruct[]>();
            foreach (MorphemicMorphologicalRule rule in slot.Rules)
            {
                IList<AffixProcessAllomorph> ruleAllomorphs;
                switch (rule)
                {
                    case AffixProcessRule affixRule:
                        ruleAllomorphs = affixRule.Allomorphs;
                        break;
                    case RealizationalAffixProcessRule realizationalRule:
                        ruleAllomorphs = realizationalRule.Allomorphs;
                        break;
                    default:
                        summary.MayAlwaysUnapply = true;
                        summary.IsPure = false;
                        continue;
                }

                foreach (AffixProcessAllomorph allomorph in ruleAllomorphs)
                {
                    if (!IsPure(allomorph))
                        summary.IsPure = false;

                    FeatureStruct[] requirements = GetRequirements(allomorph);
                    if (requirements.Length == 0)
                        summary.MayAlwaysUnapply = true;
                    else
                        allomorphs.Add(requirements);
                }
            }
            var distinct = new List<FeatureStruct>();
            summary.Allomorphs = allomorphs
                .Select(requirements =>
                    requirements
                        .Select(fs =>
                        {
                            int index = distinct.FindIndex(d => d.ValueEquals(fs));
                            if (index < 0)
                            {
                                distinct.Add(fs);
                                index = distinct.Count - 1;
                            }
                            return index;
                        })
                        .ToArray()
                )
                .ToArray();
            summary.Distinct = distinct.ToArray();
            return summary;
        }

        private static FeatureStruct[] GetRequirements(AffixProcessAllomorph allomorph)
        {
            var requirements = new List<FeatureStruct>();
            foreach (MorphologicalOutputAction action in allomorph.Rhs)
            {
                if (action is InsertSegments insertSegments)
                {
                    foreach (ShapeNode node in insertSegments.Segments.Shape)
                    {
                        if (node.Type() != HCFeatureSystem.Boundary)
                            AddRequirement(requirements, node.Annotation.FeatureStruct);
                    }
                }
                else if (action is InsertSimpleContext insertContext)
                {
                    AddRequirement(requirements, insertContext.SimpleContext.FeatureStruct);
                }
            }
            return requirements.ToArray();
        }

        // A constraint with variables is bound by earlier matches, and one that is not a segment is never
        // matched against the segments the matcher considers, so neither can be tested here.
        private static void AddRequirement(List<FeatureStruct> requirements, FeatureStruct fs)
        {
            if (fs.HasVariables || (FeatureSymbol)fs.GetValue(HCFeatureSystem.Type) != HCFeatureSystem.Segment)
                return;

            FeatureStruct copy = fs.Clone();
            copy.Freeze();
            requirements.Add(copy);
        }

        private static bool IsPure(AffixProcessAllomorph allomorph)
        {
            var copied = new HashSet<string>();
            foreach (MorphologicalOutputAction action in allomorph.Rhs)
            {
                if (action is CopyFromInput)
                    copied.Add(action.PartName);
                else if (!(action is InsertSegments || action is InsertSimpleContext))
                    return false;
            }

            foreach (Pattern<Word, ShapeNode> part in allomorph.Lhs)
            {
                if (!HasSegmentConstraint(part))
                    continue;
                if (!copied.Contains(part.Name) || MinSegments(part) == 0)
                    return false;
            }
            return true;
        }

        private static bool HasSegmentConstraint(PatternNode<Word, ShapeNode> node)
        {
            if (node is Constraint<Word, ShapeNode> constraint)
                return constraint.Type() == HCFeatureSystem.Segment;
            return node.Children.Any(HasSegmentConstraint);
        }

        // The fewest segments a successful match of the node must consume. A part matching none is captured
        // as a null range, and GenerateShape then untruncates it, which adds segments that are not copies.
        private static int MinSegments(PatternNode<Word, ShapeNode> node)
        {
            switch (node)
            {
                case Constraint<Word, ShapeNode> constraint:
                    return constraint.Type() == HCFeatureSystem.Segment ? 1 : 0;
                case Quantifier<Word, ShapeNode> quantifier:
                    return quantifier.MinOccur * quantifier.Children.Sum(MinSegments);
                case Alternation<Word, ShapeNode> alternation:
                    return alternation.Children.Count == 0 ? 0 : alternation.Children.Min(MinSegments);
                default:
                    return node.Children.Sum(MinSegments);
            }
        }

        private class SlotSummary
        {
            public bool IsPure = true;
            public bool MayAlwaysUnapply;
            public int[][] Allomorphs;
            public FeatureStruct[] Distinct;
        }
    }
}
