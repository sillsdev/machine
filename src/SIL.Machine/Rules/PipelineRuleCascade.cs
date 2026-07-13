using System.Collections.Generic;
using System.Diagnostics;
using SIL.Machine.Annotations;

namespace SIL.Machine.Rules
{
    public class PipelineRuleCascade<TData, TOffset> : RuleCascade<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        public PipelineRuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
            : base(rules) { }

        public PipelineRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, IEqualityComparer<TData> comparer)
            : base(rules, comparer) { }

        public override IEnumerable<TData> Apply(TData input)
        {
            long startTime = Stopwatch.GetTimestamp();
            var inputSet = new HashSet<TData>(Comparer) { input };
            HashSet<TData> outputSet = null;
            var tempSet = new HashSet<TData>(Comparer);
            for (int i = 0; i < Rules.Count && inputSet.Count > 0; i++)
            {
                outputSet = tempSet;
                outputSet.Clear();

                foreach (TData inData in inputSet)
                    outputSet.UnionWith(ApplyRule(Rules[i], i, inData));

                tempSet = inputSet;
                inputSet = outputSet;
            }

            AddElapsedTime(Stopwatch.GetTimestamp() - startTime);
            AddRuleStats(outputSet.Count);
            return outputSet;
        }
    }
}
