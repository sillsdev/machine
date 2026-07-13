using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    public abstract class InstrumentedRule<TData, TOffset> : IRule<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        public string Name { get; set; }
        public int InputCount;
        public int OutputCount;
        public long ElapsedTime;
        public IList<InstrumentedRule<TData, TOffset>> SubRules = new List<InstrumentedRule<TData, TOffset>>();
        private readonly object _lock = new object();

        public InstrumentedRule() { }

        /// <summary>
        /// Add sub-rules to the rule statisics.
        /// </summary>
        /// <param name="rules"></param>
        protected void AddSubRules(IEnumerable<IRule<TData, TOffset>> rules)
        {
            foreach (IRule<TData, TOffset> rule in rules)
            {
                AddSubRule(rule);
            }
        }

        protected void AddSubRule(IRule<TData, TOffset> rule)
        {
            SubRules.Add(rule as InstrumentedRule<TData, TOffset>);
        }

        /// <summary>
        /// Add input count and output count to the rule statistics.
        /// </summary>
        protected void AddRuleStats(int outputCount)
        {
            if (outputCount > 0)
            {
                Interlocked.Increment(ref InputCount);
                Interlocked.Add(ref OutputCount, outputCount);
            }
        }

        /// <summary>
        /// Add elapsed time to the rule statistics.
        /// </summary>
        protected void AddElapsedTime(long elapsedTime)
        {
            lock (_lock)
            {
                ElapsedTime += elapsedTime;
            }
        }

        /// <summary>
        /// Sort SubRules unless the order matters.
        /// </summary>
        public void SortSubRules()
        {
            if (Name == "Analysis" || Name == "Synthesis" || Name == "RuleCascade")
            {
                return;
            }
            SubRules = SubRules.OrderByDescending(rule => rule.OutputCount).ToList();
        }

        /// <summary>
        /// Clear all rule statistics.
        /// </summary>
        public void ClearStats()
        {
            InputCount = 0;
            OutputCount = 0;
            ElapsedTime = 0;
            foreach (InstrumentedRule<TData, TOffset> rule in SubRules)
            {
                rule.ClearStats();
            }
        }

        public abstract IEnumerable<TData> Apply(TData input);
    }
}
