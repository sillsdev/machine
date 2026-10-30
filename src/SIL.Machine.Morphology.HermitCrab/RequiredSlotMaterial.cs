using System;
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
    /// What a walk over the slots of one template has established about the shape, in the ordinals of the shape
    /// the walk started from. Create it with <see cref="Start"/>.
    /// </summary>
    internal struct SlotWalk
    {
        /// <summary>Stands for "any number" in <see cref="ReachLeft"/> and <see cref="ReachRight"/>.</summary>
        public const int Unbounded = int.MaxValue / 4;

        /// <summary>Every node the slots still to unapply can match lies after this ordinal.</summary>
        public int Lo;

        /// <summary>Every node the slots still to unapply can match lies before this ordinal.</summary>
        public int Hi;

        /// <summary>The most nodes that the slots walked so far can have removed from the left edge.</summary>
        public int ReachLeft;

        /// <summary>The most nodes that the slots walked so far can have removed from the right edge.</summary>
        public int ReachRight;

        /// <summary>False once a slot walked so far can reorder the nodes it copies.</summary>
        public bool Ordered;

        public static SlotWalk Start()
        {
            return new SlotWalk
            {
                Lo = -1,
                Hi = int.MaxValue,
                Ordered = true,
            };
        }
    }

    /// <summary>
    /// A necessary condition for a required affix template slot to unapply later in a template walk: some
    /// allomorph of some rule in the slot must have its inserted segments, in the order it inserts them, unify
    /// with distinct segment nodes of the shape in that order, and the nodes must be where the slots that unapply
    /// first can leave them.
    /// </summary>
    /// <remarks>
    /// The tables are built once from the template's slots, rules, and allomorphs.
    ///
    /// Presence. The analysis pattern of an allomorph holds one top-level constraint per inserted segment, in the
    /// order of the right-hand side (<see cref="InsertSegments.GenerateAnalysisLhs"/>,
    /// <see cref="InsertSimpleContext.GenerateAnalysisLhs"/>), and the rule can only unapply if the constraints
    /// unify with segment nodes of the shape in that order, each node used once. A rule is pure when every
    /// right-hand side action copies an input part or inserts material, and every left-hand part is copied by a
    /// <see cref="CopyFromInput"/> that must capture a segment, or restores no segment when it captures nothing
    /// (<see cref="AnalysisMorphologicalTransform"/> writes nothing back for an unbounded quantifier). Then
    /// <see cref="AnalysisMorphologicalTransform.GenerateShape"/> only copies input nodes and never untruncates a
    /// segment. Unification can only narrow the feature structure of a copied node, so a constraint that unifies
    /// with no node of the shape unifies with no node of the shape after any number of pure unapplications. Inside
    /// one template walk only the rules of that template's slots run, and the stratum's phonological rules ran
    /// before the walk. The test for slot j is therefore used only when every rule in the slots still to be walked
    /// after j is pure. Optional nodes count as present because the matcher can match them. Anything that cannot
    /// be summarised (a null affix, a constraint with variables that is then matched by any segment, an unknown
    /// rule type) can only weaken the test.
    ///
    /// The edge checks rely on the matchers in <see cref="AnalysisAffixProcessRule"/> and
    /// <see cref="AnalysisRealizationalAffixProcessRule"/> being anchored and filtering to segments.
    ///
    /// Order. A pure rule is ordered when it copies each part at most once and in left-hand order, so the shape
    /// it unapplies to is a subsequence of the shape it was given. An ordered rule whose inserts all come before
    /// its first copied part (after its last) is anchored on that side: the analysis matcher is anchored at both
    /// ends of the shape, so the first inserted constraint must match the first segment left once the outer slots
    /// have removed their own prefixes. A rule can remove from an edge at most as many nodes as it inserts there
    /// (<see cref="SlotWalk.ReachLeft"/>), and the nodes of a required ordered slot lie after the prefix and before
    /// the suffix it inserts (<see cref="SlotWalk.Lo"/>, <see cref="SlotWalk.Hi"/>). Both only hold while every
    /// slot walked so far is ordered, and the reach only while none can remove from the middle.
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
        /// Records that the slot is the next one to unapply, so that the slots before it are tested against what
        /// it can leave of the shape.
        /// </summary>
        public void Advance(int slot, ref SlotWalk walk)
        {
            SlotSummary summary = _slots[slot];
            if (!walk.Ordered || !summary.IsOrdered)
            {
                walk.Ordered = false;
                walk.Lo = -1;
                walk.Hi = int.MaxValue;
                walk.ReachLeft = SlotWalk.Unbounded;
                walk.ReachRight = SlotWalk.Unbounded;
                return;
            }
            walk.ReachLeft = Math.Min(SlotWalk.Unbounded, walk.ReachLeft + summary.ReachLeft);
            walk.ReachRight = Math.Min(SlotWalk.Unbounded, walk.ReachRight + summary.ReachRight);
        }

        /// <summary>
        /// True if no allomorph of the slot can unapply to a word with this shape, provided that every slot
        /// walked between this word and the slot is pure. When the slot can unapply and is ordered, narrows the
        /// window of the walk for the slots before it.
        /// </summary>
        /// <param name="slot">The slot to test.</param>
        /// <param name="shape">The shape of the partial.</param>
        /// <param name="walk">What the slots walked since the shape was taken have established.</param>
        /// <param name="unifications">Incremented by the feature structure unifications performed.</param>
        public bool CannotUnapply(int slot, Shape shape, ref SlotWalk walk, ref long unifications)
        {
            SlotSummary summary = _slots[slot];
            if (summary.MayAlwaysUnapply)
                return false;

            // The window is only narrowed for a slot that has slots after it in the walk, and that needs every
            // passing allomorph, not the first.
            bool narrow = walk.Ordered && summary.IsOrdered && slot > 0;
            int newLo = int.MaxValue;
            int newHi = -1;
            bool any = false;
            foreach (AllomorphSummary allomorph in summary.Allomorphs)
            {
                int prefixEnd = -1;
                int suffixStart = int.MaxValue;
                bool found = walk.Ordered
                    ? Embeds(allomorph, shape, ref walk, ref unifications, out prefixEnd, out suffixStart)
                    : AllPresent(allomorph, shape, ref unifications);
                if (!found)
                    continue;

                if (!narrow)
                    return false;
                any = true;
                newLo = Math.Min(newLo, allomorph.Pre > 0 ? prefixEnd : walk.Lo);
                newHi = Math.Max(newHi, allomorph.Post > 0 ? suffixStart : walk.Hi);
            }
            if (!any)
                return true;

            walk.Lo = newLo;
            walk.Hi = newHi;
            return false;
        }

        // AnalysisAffixProcessRule and AnalysisRealizationalAffixProcessRule use an edge-anchored,
        // segment-only matcher. Optional nodes do not count against reach because the matcher may skip them.
        private static bool Embeds(
            AllomorphSummary allomorph,
            Shape shape,
            ref SlotWalk walk,
            ref long unifications,
            out int prefixEnd,
            out int suffixStart
        )
        {
            prefixEnd = -1;
            suffixStart = int.MaxValue;
            FeatureStruct[] sequence = allomorph.Sequence;
            int front = sequence.Length - allomorph.Post;
            int forwardEnd = -1;

            int next = 0;
            int before = 0;
            int ordinal = -1;
            foreach (ShapeNode node in shape)
            {
                if (next == front)
                    break;
                ordinal++;
                if (ordinal >= walk.Hi)
                    break;

                bool segment = node.Type() == HCFeatureSystem.Segment;
                if (segment && ordinal > walk.Lo)
                {
                    if (next < allomorph.Pre && before > Math.Min(SlotWalk.Unbounded, walk.ReachLeft + next))
                        return false;

                    FeatureStruct required = sequence[next];
                    if (required == null || IsUnifiable(node, required, ref unifications))
                    {
                        if (next == allomorph.Pre - 1)
                            prefixEnd = ordinal;
                        forwardEnd = ordinal;
                        next++;
                    }
                }
                if (segment && !node.Annotation.Optional)
                    before++;
            }
            if (next < front)
                return false;
            if (allomorph.Post == 0)
                return true;

            int taken = 0;
            int after = 0;
            ordinal = shape.Count;
            for (ShapeNode node = shape.Last; node != null && taken < allomorph.Post; node = node.Prev)
            {
                ordinal--;
                if (ordinal <= walk.Lo || ordinal <= forwardEnd)
                    break;

                bool segment = node.Type() == HCFeatureSystem.Segment;
                if (segment && ordinal < walk.Hi)
                {
                    if (after > Math.Min(SlotWalk.Unbounded, walk.ReachRight + taken))
                        return false;

                    FeatureStruct required = sequence[sequence.Length - 1 - taken];
                    if (required == null || IsUnifiable(node, required, ref unifications))
                    {
                        suffixStart = ordinal;
                        taken++;
                    }
                }
                if (segment && !node.Annotation.Optional)
                    after++;
            }
            return taken == allomorph.Post;
        }

        // Once a slot that can reorder nodes has been walked the order of the shape says nothing about the order of
        // the shape the slot unapplies to, so each constraint is only looked for on its own.
        private static bool AllPresent(AllomorphSummary allomorph, Shape shape, ref long unifications)
        {
            foreach (FeatureStruct required in allomorph.Sequence)
            {
                if (required == null)
                    continue;

                bool found = false;
                foreach (ShapeNode node in shape)
                {
                    if (node.Type() == HCFeatureSystem.Segment && IsUnifiable(node, required, ref unifications))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
        }

        private static bool IsUnifiable(ShapeNode node, FeatureStruct required, ref long unifications)
        {
            unifications++;
            return node.Annotation.FeatureStruct.IsUnifiable(required, false, null);
        }

        private static SlotSummary Summarize(AffixTemplateSlot slot)
        {
            var summary = new SlotSummary();
            var allomorphs = new List<AllomorphSummary>();
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
                        summary.IsOrdered = false;
                        summary.ReachLeft = SlotWalk.Unbounded;
                        summary.ReachRight = SlotWalk.Unbounded;
                        continue;
                }

                foreach (AffixProcessAllomorph allomorph in ruleAllomorphs)
                {
                    if (
                        allomorph.Lhs.Any(HasUntypedConstraint)
                        || allomorph.Rhs.Where(IsInsertion).SelectMany(InsertedConstraints).Any(HasNoType)
                    )
                    {
                        summary.MayAlwaysUnapply = true;
                        summary.IsPure = false;
                        summary.IsOrdered = false;
                        summary.ReachLeft = SlotWalk.Unbounded;
                        summary.ReachRight = SlotWalk.Unbounded;
                        continue;
                    }

                    bool pure = IsPure(allomorph);
                    if (!pure)
                        summary.IsPure = false;
                    if (!pure || !IsOrdered(allomorph))
                        summary.IsOrdered = false;

                    AllomorphSummary allomorphSummary = Summarize(allomorph);
                    summary.ReachLeft = Math.Max(summary.ReachLeft, allomorphSummary.ReachLeft);
                    summary.ReachRight = Math.Max(summary.ReachRight, allomorphSummary.ReachRight);
                    if (allomorphSummary.Sequence.Length == 0)
                        summary.MayAlwaysUnapply = true;
                    else
                        allomorphs.Add(allomorphSummary);
                }
            }
            summary.Allomorphs = allomorphs.ToArray();
            return summary;
        }

        // Pre is the inserts before the first non-insertion, Post those after the last; any between is an infix.
        // A non-segment constraint matches nothing the matcher sees, so skipping it only weakens the test.
        private static AllomorphSummary Summarize(AffixProcessAllomorph allomorph)
        {
            var sequence = new List<FeatureStruct>();
            IList<MorphologicalOutputAction> rhs = allomorph.Rhs;
            int first = -1;
            int last = -1;
            for (int i = 0; i < rhs.Count; i++)
            {
                if (IsInsertion(rhs[i]))
                    continue;
                if (first < 0)
                    first = i;
                last = i;
            }

            int pre = 0;
            int post = 0;
            bool infix = false;
            for (int i = 0; i < rhs.Count; i++)
            {
                if (!IsInsertion(rhs[i]))
                    continue;

                int before = sequence.Count;
                foreach (FeatureStruct fs in InsertedConstraints(rhs[i]))
                {
                    if ((FeatureSymbol)fs.GetValue(HCFeatureSystem.Type) != HCFeatureSystem.Segment)
                        continue;

                    // A constraint with variables is bound by earlier matches, so any segment stands for it.
                    FeatureStruct copy = null;
                    if (!fs.HasVariables)
                    {
                        copy = fs.Clone();
                        copy.Freeze();
                    }
                    sequence.Add(copy);
                }
                int added = sequence.Count - before;
                if (added > 0 && (first < 0 || (i > first && i < last)))
                    infix = true;
                else if (i < first)
                    pre += added;
                else
                    post += added;
            }

            var summary = new AllomorphSummary { Sequence = sequence.ToArray() };
            if (infix)
            {
                summary.ReachLeft = SlotWalk.Unbounded;
                summary.ReachRight = SlotWalk.Unbounded;
            }
            else
            {
                summary.Pre = pre;
                summary.Post = post;
                summary.ReachLeft = pre;
                summary.ReachRight = post;
            }
            return summary;
        }

        private static bool IsInsertion(MorphologicalOutputAction action)
        {
            return action is InsertSegments || action is InsertSimpleContext;
        }

        private static IEnumerable<FeatureStruct> InsertedConstraints(MorphologicalOutputAction action)
        {
            if (action is InsertSegments insertSegments)
            {
                foreach (ShapeNode node in insertSegments.Segments.Shape)
                {
                    if (node.Type() != HCFeatureSystem.Boundary)
                        yield return node.Annotation.FeatureStruct;
                }
            }
            else if (action is InsertSimpleContext insertContext)
            {
                yield return insertContext.SimpleContext.FeatureStruct;
            }
        }

        private static bool IsPure(AffixProcessAllomorph allomorph)
        {
            var copied = new HashSet<string>();
            foreach (MorphologicalOutputAction action in allomorph.Rhs)
            {
                if (action is CopyFromInput)
                    copied.Add(action.PartName);
                else if (!IsInsertion(action))
                    return false;
            }

            foreach (Pattern<Word, ShapeNode> part in allomorph.Lhs)
            {
                if (RestoredSegments(part) == 0)
                    continue;
                if (!copied.Contains(part.Name) || MinSegments(part) == 0)
                    return false;
            }
            return true;
        }

        // GenerateShape writes parts in left-hand order, so copying them in another order, or one twice, can
        // reorder or repeat nodes.
        private static bool IsOrdered(AffixProcessAllomorph allomorph)
        {
            int previous = -1;
            foreach (MorphologicalOutputAction action in allomorph.Rhs)
            {
                if (!(action is CopyFromInput))
                    continue;

                int index = allomorph.Lhs.ToList().FindIndex(part => part.Name == action.PartName);
                if (index <= previous)
                    return false;
                previous = index;
            }
            return true;
        }

        private static bool HasNoType(FeatureStruct fs)
        {
            return !fs.TryGetValue(HCFeatureSystem.Type, out SymbolicFeatureValue _);
        }

        private static bool HasUntypedConstraint(PatternNode<Word, ShapeNode> node)
        {
            return (node is Constraint<Word, ShapeNode> constraint && HasNoType(constraint.FeatureStruct))
                || node.Children.Any(HasUntypedConstraint);
        }

        // The segment nodes that AnalysisMorphologicalTransform.Untruncate writes for a part that was not
        // captured. An unbounded quantifier writes none, as in Mbugwe's vowel-fusion slots.
        private static int RestoredSegments(PatternNode<Word, ShapeNode> node)
        {
            int total = 0;
            foreach (PatternNode<Word, ShapeNode> child in node.Children)
            {
                if (child is Constraint<Word, ShapeNode> constraint && constraint.Type() == HCFeatureSystem.Segment)
                    total++;
                else if (child is Quantifier<Word, ShapeNode> quantifier)
                    total += Math.Max(0, quantifier.MaxOccur) * RestoredSegments(quantifier);
                else
                    total += RestoredSegments(child);
            }
            return total;
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

        private class AllomorphSummary
        {
            public FeatureStruct[] Sequence;
            public int Pre;
            public int Post;
            public int ReachLeft;
            public int ReachRight;
        }

        private class SlotSummary
        {
            public bool IsPure = true;
            public bool IsOrdered = true;
            public bool MayAlwaysUnapply;
            public int ReachLeft;
            public int ReachRight;
            public AllomorphSummary[] Allomorphs;
        }
    }
}
