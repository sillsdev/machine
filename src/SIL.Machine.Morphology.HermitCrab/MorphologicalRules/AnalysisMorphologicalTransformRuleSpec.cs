using System;
using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
    public abstract class AnalysisMorphologicalTransformRuleSpec
        : AnalysisMorphologicalTransform,
            IPatternRuleSpec<Word, ShapeNode>
    {
        protected AnalysisMorphologicalTransformRuleSpec(
            IEnumerable<Pattern<Word, ShapeNode>> lhs,
            IList<MorphologicalOutputAction> rhs
        )
            : base(lhs, rhs) { }

        /// <summary>
        /// The morpher whose <see cref="Morpher.PruneDisagreeingCopies"/> setting governs pruning; unset means no pruning.
        /// </summary>
        internal Morpher PruningMorpher { get; set; }

        internal bool CopiesMayAgree(Match<Word, ShapeNode> match)
        {
            return PruningMorpher == null
                || !PruningMorpher.PruneDisagreeingCopies
                || !HasRepeatedParts
                || !HasDisagreeingCopies(match);
        }

        public bool IsApplicable(Word input)
        {
            return true;
        }

        protected bool IsPartCaptured(Match<Word, ShapeNode> match, string partName)
        {
            int count;
            if (CapturedParts.TryGetValue(partName, out count))
            {
                for (int i = 0; i < count; i++)
                {
                    if (match.GroupCaptures.Captured(GetGroupName(partName, i)))
                        return true;
                }
            }
            return false;
        }

        public abstract Word ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match);
    }
}
