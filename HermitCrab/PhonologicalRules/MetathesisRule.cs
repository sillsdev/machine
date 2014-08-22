using System.Collections.Generic;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	/// <summary>
	/// This class represents a metathesis rule. Metathesis rules are phonological rules that
	/// reorder segments.
	/// </summary>
	public class MetathesisRule : HCRuleBase, IPhonologicalRule
	{
		private readonly List<string> _groupOrder;

		public MetathesisRule()
		{
			Pattern = Pattern<Word, ShapeNode>.New().Value;
			_groupOrder = new List<string>();
		}

		public Direction Direction { get; set; }

		public Pattern<Word, ShapeNode> Pattern { get; set; }

		public IList<string> GroupOrder
		{
			get { return _groupOrder; }
		}

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