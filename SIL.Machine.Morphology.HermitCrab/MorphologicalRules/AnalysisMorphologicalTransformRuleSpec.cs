using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
	public abstract class AnalysisMorphologicalTransformRuleSpec : AnalysisMorphologicalTransform, IPatternRuleSpec<Word, ShapeNode>
	{
		protected AnalysisMorphologicalTransformRuleSpec(IEnumerable<Pattern<Word, ShapeNode>> lhs, IList<MorphologicalOutputAction> rhs)
			: base(lhs, rhs)
		{
		}

		public bool IsApplicable(Word input)
		{
			return true;
		}

		public abstract ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output);
	}
}
