using System.Collections.Generic;
using SIL.Machine.Annotations;

namespace SIL.Machine.Rules
{
    /// <summary>
    /// One observed "context" a rule succeeded under -- e.g. the part of speech of the word it applied to,
    /// which allomorph/subrule fired, or whether the input was still a bare root. Grammar-constraint mining
    /// (parse-optimization.md-adjacent: use runtime evidence to suggest tightenable rule declarations) needs
    /// both the count (300 vs 4 is the signal) and a handful of real words (so a linguist can eyeball the 4
    /// counterexamples and judge whether they're legitimate or a grammar bug).
    /// </summary>
    public class RuleBucket
    {
        public const int MaxExamples = 10;

        public long Count;
        public readonly List<string> Examples = new List<string>();

        public void Record(string example)
        {
            Count++;
            if (Examples.Count < MaxExamples)
                Examples.Add(example);
        }
    }

    /// <summary>
    /// This class instruments IRules.
    /// Statistics are stored in InputCount, OutputCount, and ElapsedTime.
    /// The rules update the statistics when Apply is called.
    /// Name and SubRules are filled in when the rule is created.
    /// Rules that can distinguish *why* a given application succeeded (which allomorph, which category,
    /// whether the target was a bare stem, ...) additionally record named buckets via RecordBucket --
    /// see Morpher.AccumulateRuleStats for how these survive across a whole corpus run instead of being
    /// cleared per word.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <typeparam name="TOffset"></typeparam>
    public abstract class InstrumentedRule<TData, TOffset> : IRule<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        public string Name { get; set; }
        public int InputCount;
        public int OutputCount;
        public int SuccessCount;
        public long ElapsedTime;
        public IList<InstrumentedRule<TData, TOffset>> SubRules = new List<InstrumentedRule<TData, TOffset>>();

        // Keyed by an arbitrary "bucket group" name (e.g. "category", "allomorph") so one rule can report
        // several independent breakdowns without them being conflated into a single key space.
        public IDictionary<string, Dictionary<string, RuleBucket>> BucketGroups =
            new Dictionary<string, Dictionary<string, RuleBucket>>();

        // Generic-arity backtick suffix (e.g. "CombinationRuleCascade`2") stripped so reports read as
        // "CombinationRuleCascade" -- callers that want something more specific (a stratum/template/morpheme
        // name) still overwrite Name after construction.
        protected InstrumentedRule()
        {
            string typeName = GetType().Name;
            int tickIndex = typeName.IndexOf('`');
            Name = tickIndex < 0 ? typeName : typeName.Substring(0, tickIndex);
        }

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

        protected void AddRuleStats(int outputCount)
        {
            InputCount++;
            OutputCount += outputCount;
            if (outputCount > 0)
                SuccessCount++;
        }

        // group examples: "category" ("Verb", "Noun", ...), "allomorph" ("0", "1", ...), "stemName",
        // "rootDirect" ("true"/"false"). Callers pick whichever groups are meaningful for that rule type.
        protected void RecordBucket(string group, string key, string example)
        {
            if (!BucketGroups.TryGetValue(group, out Dictionary<string, RuleBucket> buckets))
            {
                buckets = new Dictionary<string, RuleBucket>();
                BucketGroups[group] = buckets;
            }
            if (!buckets.TryGetValue(key, out RuleBucket bucket))
            {
                bucket = new RuleBucket();
                buckets[key] = bucket;
            }
            bucket.Record(example);
        }

        public void ClearStats()
        {
            InputCount = 0;
            OutputCount = 0;
            SuccessCount = 0;
            ElapsedTime = 0;
            BucketGroups.Clear();
            foreach (var rule in SubRules)
            {
                rule?.ClearStats();
            }
        }

        public abstract IEnumerable<TData> Apply(TData input);
    }
}
