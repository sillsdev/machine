using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class BacktrackingPatternRule : IterativePatternRule<Word, ShapeNode>
	{
		public BacktrackingPatternRule(SpanFactory<ShapeNode> spanFactory, IPatternRuleSpec<Word, ShapeNode> ruleSpec)
			: base(spanFactory, ruleSpec)
		{
		}

		public BacktrackingPatternRule(SpanFactory<ShapeNode> spanFactory, IPatternRuleSpec<Word, ShapeNode> ruleSpec, MatcherSettings<ShapeNode> matcherSettings)
			: base(spanFactory, ruleSpec, matcherSettings)
		{
		}

		protected override IEnumerable<Word> ApplyImpl(Word input, ShapeNode start)
		{
			Word output = base.ApplyImpl(input, start).SingleOrDefault();
			if (output != null)
			{
				output.ResetDirty();
				return output.ToEnumerable();
			}

			return Enumerable.Empty<Word>();
		}
	}
}
