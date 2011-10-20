using System;
using System.Collections.Generic;
using SIL.APRE;
using SIL.APRE.Transduction;

namespace SIL.HermitCrab
{
	public class AffixProcessAnalysisRule : RuleCascadeBase<Word, ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;

		public AffixProcessAnalysisRule(SpanFactory<ShapeNode> spanFactory)
			: base(RuleCascadeOrder.Linear)
		{
			_spanFactory = spanFactory;
		}

		public void AddAllomorph(AffixProcessAllomorph allomorph)
		{
			AddRuleInternal(new AnalysisAffixPatternRule(_spanFactory, allomorph), true);
		}

		public override bool IsApplicable(Word input)
		{
			throw new NotImplementedException();
		}

		public override bool Apply(Word input, out IEnumerable<Word> output)
		{
			bool result = base.Apply(input, out output);
			// TODO: add trace record here
			return result;
		}

		protected override bool ApplyRule(IRule<Word, ShapeNode> rule, Word input, out IEnumerable<Word> output)
		{
			if (base.ApplyRule(rule, input, out output))
			{
				// TODO: do lexical lookup here
				return true;
			}

			return false;
		}
	}
}
