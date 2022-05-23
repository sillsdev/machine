using System;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Rules
{
    public class DefaultPatternRuleSpec<TData, TOffset> : IPatternRuleSpec<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        private readonly Pattern<TData, TOffset> _pattern;
        private readonly Func<PatternRule<TData, TOffset>, Match<TData, TOffset>, TData> _func;
        private readonly Func<TData, bool> _applicable;

        public DefaultPatternRuleSpec(
            Pattern<TData, TOffset> pattern,
            Func<PatternRule<TData, TOffset>, Match<TData, TOffset>, TData> func
        ) : this(pattern, func, ann => true) { }

        public DefaultPatternRuleSpec(
            Pattern<TData, TOffset> pattern,
            Func<PatternRule<TData, TOffset>, Match<TData, TOffset>, TData> func,
            Func<TData, bool> applicable
        )
        {
            _pattern = pattern;
            _func = func;
            _applicable = applicable;
        }

        public Pattern<TData, TOffset> Pattern
        {
            get { return _pattern; }
        }

        public bool IsApplicable(TData input)
        {
            return _applicable(input);
        }

        public TData ApplyRhs(PatternRule<TData, TOffset> rule, Match<TData, TOffset> match)
        {
            return _func(rule, match);
        }
    }
}
