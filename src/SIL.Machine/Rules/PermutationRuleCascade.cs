using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Rules
{
    public class PermutationRuleCascade<TData, TOffset> : RuleCascade<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        public PermutationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
            : base(rules) { }

        public PermutationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
            : base(rules, comparer) { }

        public PermutationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp)
            : base(rules, multiApp) { }

        public PermutationRuleCascade(
            IEnumerable<IRule<TData, TOffset>> rules,
            bool multiApp,
            IEqualityComparer<TData> comparer
        )
            : base(rules, multiApp, comparer) { }

        /// <summary>
        /// Caps how many nested rule (re-)applications a single branch may descend through, on top of
        /// the base class's input==output infinite-loop guard (which a rule whose output never exactly
        /// repeats its input — e.g. one that keeps growing the shape — sails past). -1 = unlimited, the
        /// default, so existing consumers see no behavior change.
        /// </summary>
        public int MaxDepth { get; set; } = -1;

        public override IEnumerable<TData> Apply(TData input)
        {
            var output = new HashSet<TData>(Comparer);
            ApplyRules(input, 0, 0, output);
            return output;
        }

        private void ApplyRules(TData input, int ruleIndex, int depth, HashSet<TData> output)
        {
            bool descend = MaxDepth < 0 || depth < MaxDepth;
            for (int i = ruleIndex; i < Rules.Count; i++)
            {
                foreach (TData result in ApplyRule(Rules[i], i, input))
                {
                    // avoid infinite loop
                    if (descend && (!MultipleApplication || !Comparer.Equals(input, result)))
                        ApplyRules(result, MultipleApplication ? i : i + 1, depth + 1, output);
                    output.Add(result);
                }
            }
        }
    }
}
