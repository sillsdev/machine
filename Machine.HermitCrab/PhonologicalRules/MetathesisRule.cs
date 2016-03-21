using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.HermitCrab.PhonologicalRules
{
	/// <summary>
	/// This class represents a metathesis rule. Metathesis rules are phonological rules that
	/// reorder segments.
	/// </summary>
	public class MetathesisRule : HCRuleBase, IPhonologicalRule
	{
		public MetathesisRule()
		{
			Pattern = Pattern<Word, ShapeNode>.New().Value;
		}

		public Direction Direction { get; set; }

		public Pattern<Word, ShapeNode> Pattern { get; set; }

		public string LeftGroupName { get; set; }

		public string RightGroupName { get; set; }

		public override IRule<Word, ShapeNode> CompileAnalysisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new AnalysisMetathesisRule(spanFactory, morpher, this);
		}

		public override IRule<Word, ShapeNode> CompileSynthesisRule(SpanFactory<ShapeNode> spanFactory, Morpher morpher)
		{
			return new SynthesisMetathesisRule(spanFactory, morpher, this);
		}
	}
}