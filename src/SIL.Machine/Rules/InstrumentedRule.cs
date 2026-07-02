using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Rules
{
    /// <summary>
    /// This class instruments IRules.
    /// Statistics are stored in InputCount, OutputCount, and ElapsedTime.
    /// The rules update the statistics when Apply is called.
    /// Name and SubRules are filled in when the rule is created.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <typeparam name="TOffset"></typeparam>
    public abstract class InstrumentedRule<TData, TOffset>: IRule<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        public string Name { get; set; }
        public int InputCount;
        public int OutputCount;
        public long ElapsedTime;
        public IList<InstrumentedRule<TData, TOffset>> SubRules = new List<InstrumentedRule<TData, TOffset>>();

        public InstrumentedRule() { }

        protected void AddSubRules(IEnumerable<IRule<TData, TOffset>> rules)
        {
            foreach (IRule<TData, TOffset> rule in rules)
            {
                AddSubRule(rule);
            }
        }

        protected void AddSubRule(IRule<TData, TOffset>rule)
        {
            SubRules.Add(rule as InstrumentedRule<TData, TOffset>);
        }

        protected void AddRuleStats(int outputCount)
        {
            InputCount++;
            OutputCount += outputCount;
        }

        public void ClearStats()
        {
            InputCount = 0;
            OutputCount = 0;
            ElapsedTime = 0;
            foreach (var rule in SubRules)
            {
                rule.ClearStats();
            }
        }

        public abstract IEnumerable<TData> Apply(TData input);
    }
}
