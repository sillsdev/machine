using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public class BacktrackingPatternRule : PatternRule<Word, ShapeNode>
	{
		public BacktrackingPatternRule(SpanFactory<ShapeNode> spanFactory, IPatternRuleSpec<Word, ShapeNode> ruleSpec)
			: base(spanFactory, ruleSpec)
		{
		}

		public BacktrackingPatternRule(SpanFactory<ShapeNode> spanFactory, IPatternRuleSpec<Word, ShapeNode> ruleSpec, ApplicationMode appMode)
			: base(spanFactory, ruleSpec, appMode)
		{
		}

		public BacktrackingPatternRule(SpanFactory<ShapeNode> spanFactory, IPatternRuleSpec<Word, ShapeNode> ruleSpec, MatcherSettings<ShapeNode> matcherSettings)
			: base(spanFactory, ruleSpec, matcherSettings)
		{
		}

		public BacktrackingPatternRule(SpanFactory<ShapeNode> spanFactory, IPatternRuleSpec<Word, ShapeNode> ruleSpec, ApplicationMode appMode, MatcherSettings<ShapeNode> matcherSettings)
			: base(spanFactory, ruleSpec, appMode, matcherSettings)
		{
		}

		public override IEnumerable<Word> Apply(Word input, ShapeNode start)
		{
			Word output = base.Apply(input, start).SingleOrDefault();
			if (output != null)
			{
				if (ApplicationMode == ApplicationMode.Iterative)
				{
					foreach (ShapeNode node in output.Shape)
						node.SetDirty(false);
				}
				return output.ToEnumerable();
			}

			return Enumerable.Empty<Word>();
		}
	}
}
