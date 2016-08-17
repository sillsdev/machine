using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.HermitCrab.PhonologicalRules
{
	public abstract class PhonologicalPatternRule : IRule<Word, ShapeNode>
	{
		private readonly SpanFactory<ShapeNode> _spanFactory;
		private readonly IPhonologicalPatternRuleSpec _ruleSpec;
		private readonly Matcher<Word, ShapeNode> _matcher; 

		protected PhonologicalPatternRule(SpanFactory<ShapeNode> spanFactory, IPhonologicalPatternRuleSpec ruleSpec, MatcherSettings<ShapeNode> matcherSettings)
		{
			_spanFactory = spanFactory;
			_ruleSpec = ruleSpec;
			_matcher = new Matcher<Word, ShapeNode>(spanFactory, _ruleSpec.Pattern, matcherSettings);
		}

		public SpanFactory<ShapeNode> SpanFactory
		{
			get { return _spanFactory; }
		}

		public Matcher<Word, ShapeNode> Matcher
		{
			get { return _matcher; }
		}

		public IPhonologicalPatternRuleSpec RuleSpec
		{
			get { return _ruleSpec; }
		}

		public abstract IEnumerable<Word> Apply(Word input);
	}
}
