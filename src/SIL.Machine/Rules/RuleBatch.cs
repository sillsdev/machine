using System.Collections.Generic;
using SIL.Extensions;
using SIL.Machine.Annotations;

namespace SIL.Machine.Rules
{
    public class RuleBatch<TData, TOffset> : InstrumentedRule<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        private readonly List<IRule<TData, TOffset>> _rules;
        private readonly bool _disjunctive;
        private readonly IEqualityComparer<TData> _comparer;

        public RuleBatch(IEnumerable<IRule<TData, TOffset>> rules)
            : this(rules, EqualityComparer<TData>.Default) { }

        public RuleBatch(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
            : this(rules, true, comparer) { }

        public RuleBatch(IEnumerable<IRule<TData, TOffset>> rules, bool disjunctive)
            : this(rules, disjunctive, EqualityComparer<TData>.Default) { }

        public RuleBatch(IEnumerable<IRule<TData, TOffset>> rules, bool disjunctive, IEqualityComparer<TData> comparer)
        {
            Name = "RuleBatch";
            _rules = new List<IRule<TData, TOffset>>(rules);
            _disjunctive = disjunctive;
            _comparer = comparer;
            AddSubRules(_rules);
        }

        public IReadOnlyList<IRule<TData, TOffset>> Rules
        {
            get { return _rules.ToReadOnlyList(); }
        }

        public IEqualityComparer<TData> Comparer
        {
            get { return _comparer; }
        }

        public bool IsDisjunctive
        {
            get { return _disjunctive; }
        }

        public override IEnumerable<TData> Apply(TData input)
        {
            var output = new HashSet<TData>(_comparer);
            foreach (IRule<TData, TOffset> rule in _rules)
            {
                output.UnionWith(rule.Apply(input));
                if (_disjunctive && output.Count > 0)
                    return output;
            }

            AddRuleStats(output.Count);
            return output;
        }
    }
}
