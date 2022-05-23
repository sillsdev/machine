using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Rules
{
    public class PatternRule<TData, TOffset> : IRule<TData, TOffset> where TData : IAnnotatedData<TOffset>
    {
        private readonly IPatternRuleSpec<TData, TOffset> _ruleSpec;
        private readonly Matcher<TData, TOffset> _matcher;

        public PatternRule(IPatternRuleSpec<TData, TOffset> ruleSpec) : this(ruleSpec, new MatcherSettings<TOffset>())
        { }

        public PatternRule(IPatternRuleSpec<TData, TOffset> ruleSpec, MatcherSettings<TOffset> matcherSettings)
        {
            _ruleSpec = ruleSpec;
            _matcher = new Matcher<TData, TOffset>(_ruleSpec.Pattern, matcherSettings);
        }

        public Matcher<TData, TOffset> Matcher
        {
            get { return _matcher; }
        }

        public IPatternRuleSpec<TData, TOffset> RuleSpec
        {
            get { return _ruleSpec; }
        }

        public IEnumerable<TData> Apply(TData input)
        {
            return Apply(input, input.Range.GetStart(_matcher.Direction));
        }

        public IEnumerable<TData> Apply(TData input, TOffset start)
        {
            if (!_ruleSpec.IsApplicable(input) || input.Annotations.Count == 0)
                return Enumerable.Empty<TData>();

            return ApplyImpl(input, start);
        }

        protected virtual IEnumerable<TData> ApplyImpl(TData input, TOffset start)
        {
            Match<TData, TOffset> match = _matcher.Match(input, start);
            if (match.Success)
                return _ruleSpec.ApplyRhs(this, match).ToEnumerable();
            return Enumerable.Empty<TData>();
        }
    }
}
