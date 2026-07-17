using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Annotations;

namespace SIL.Machine.Rules
{
    public class ParallelPermutationRuleCascade<TData, TOffset> : PermutationRuleCascade<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        public ParallelPermutationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules)
            : base(rules) { }

        public ParallelPermutationRuleCascade(
            IEnumerable<IRule<TData, TOffset>> rules,
            IEqualityComparer<TData> comparer
        )
            : base(rules, comparer) { }

        public ParallelPermutationRuleCascade(IEnumerable<IRule<TData, TOffset>> rules, bool multiApp)
            : base(rules, multiApp) { }

        public ParallelPermutationRuleCascade(
            IEnumerable<IRule<TData, TOffset>> rules,
            bool multiApp,
            IEqualityComparer<TData> comparer
        )
            : base(rules, multiApp, comparer) { }

        public override IEnumerable<TData> Apply(TData input)
        {
            var output = new ConcurrentStack<TData>();
            var from = new ConcurrentStack<Tuple<TData, int>>();
            from.Push(Tuple.Create(input, 0));
            var to = new ConcurrentStack<Tuple<TData, int>>();
            Exception exception = null;
            int alternativeCount = 0;
            while (!from.IsEmpty)
            {
                to.Clear();
                Parallel.ForEach(
                    from,
                    (work, state) =>
                    {
                        try
                        {
                            for (int i = work.Item2; i < Rules.Count; i++)
                            {
                                TData[] results = ApplyRule(Rules[i], i, work.Item1).ToArray();
                                if (results.Length > 0)
                                {
                                    output.PushRange(results);
                                    CheckMaxAlternatives(Interlocked.Add(ref alternativeCount, results.Length));

                                    Tuple<TData, int>[] workItems = results
                                        .Where(res => !MultipleApplication || !Comparer.Equals(work.Item1, res))
                                        .Select(res => Tuple.Create(res, MultipleApplication ? i : i + 1))
                                        .ToArray();
                                    if (workItems.Length > 0)
                                        to.PushRange(workItems);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            state.Stop();
                            exception = ex;
                        }
                    }
                );
                if (exception != null)
                    throw exception;
                ConcurrentStack<Tuple<TData, int>> temp = from;
                from = to;
                to = temp;
            }

            return output.Distinct(Comparer);
        }
    }
}
