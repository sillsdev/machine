using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Rules
{
    public class CombinationRuleCascade<TData, TOffset> : RuleCascade<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        public CombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
            : base(rules) { }

        public CombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
            : base(rules, comparer) { }

        public CombinationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp)
            : base(rules, multiApp) { }

        public CombinationRuleCascade(
            IEnumerable<IRule<TData, TOffset>> rules,
            bool multiApp,
            IEqualityComparer<TData> comparer
        )
            : base(rules, multiApp, comparer) { }

        public override IEnumerable<TData> Apply(TData input)
        {
            var output = new HashSet<TData>(Comparer);
            // In multiApp mode a word's expansion depends only on the word (not the path taken
            // to reach it), so we can memoize which words have already been expanded and skip
            // re-descending them. This collapses the redundant combinatorial re-exploration to a
            // DAG without changing the output set. Not valid when rule history matters
            // (!MultipleApplication), so the memo is only used in multiApp mode.
            HashSet<TData> expanded = MultipleApplication ? new HashSet<TData>(Comparer) : null;
            // Seed with the initial input so a cycle that returns to it (A->B->A) doesn't re-expand it.
            expanded?.Add(input);
            ApplyRules(input, !MultipleApplication ? new HashSet<int>() : null, output, expanded);
            return output;
        }

        private void ApplyRules(TData input, HashSet<int> rulesApplied, HashSet<TData> output, HashSet<TData> expanded)
        {
            for (int i = 0; i < Rules.Count; i++)
            {
                if ((rulesApplied == null || !rulesApplied.Contains(i)))
                {
                    foreach (TData result in ApplyRule(Rules[i], i, input))
                    {
                        // avoid infinite loop; in multiApp mode also skip words already expanded
                        if (!Comparer.Equals(input, result) && (expanded == null || expanded.Add(result)))
                        {
                            ApplyRules(
                                result,
                                rulesApplied == null ? null : new HashSet<int>(rulesApplied) { i },
                                output,
                                expanded
                            );
                        }

                        output.Add(result);
                    }
                }
            }
        }
    }
}
