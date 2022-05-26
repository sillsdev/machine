using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.PhonologicalRules
{
    public abstract class PhonologicalPatternRule : IRule<Word, ShapeNode>
    {
        private readonly IPhonologicalPatternRuleSpec _ruleSpec;
        private readonly Matcher<Word, ShapeNode> _matcher;

        protected PhonologicalPatternRule(
            IPhonologicalPatternRuleSpec ruleSpec,
            MatcherSettings<ShapeNode> matcherSettings
        )
        {
            _ruleSpec = ruleSpec;
            _matcher = new Matcher<Word, ShapeNode>(_ruleSpec.Pattern, matcherSettings);
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
