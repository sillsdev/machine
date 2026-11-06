using System;
using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Rules
{
    public class LinearRuleCascade<TData, TOffset> : RuleCascade<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        public LinearRuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
            : base(rules) { }

        public LinearRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
            : base(rules, comparer) { }

        public LinearRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp)
            : base(rules, multiApp) { }

        public LinearRuleCascade(
            IEnumerable<IRule<TData, TOffset>> rules,
            bool multiApp,
            IEqualityComparer<TData> comparer
        )
            : base(rules, multiApp, comparer) { }

        public override IEnumerable<TData> Apply(
            TData input,
            Func<IEnumerable<TData>, IEnumerable<TData>> filterResults
        )
        {
            var output = new HashSet<TData>(Comparer);
            ApplyRules(input, 0, output, filterResults);
            return output;
        }

        private bool ApplyRules(
            TData input,
            int ruleIndex,
            HashSet<TData> output,
            Func<IEnumerable<TData>, IEnumerable<TData>> filterResults
        )
        {
            for (int i = ruleIndex; i < Rules.Count; i++)
            {
                bool applied = false;
                foreach (TData result in filterResults(ApplyRule(Rules[i], i, input)))
                {
                    // avoid infinite loop
                    if (!MultipleApplication || !Comparer.Equals(input, result))
                    {
                        if (ApplyRules(result, MultipleApplication ? i : i + 1, output, filterResults))
                            output.Add(result);
                    }
                    else
                    {
                        output.Add(result);
                    }
                    CheckMaxAlternatives(output.Count);
                    applied = true;
                }

                if (applied)
                    return false;
            }
            return true;
        }
    }
}
