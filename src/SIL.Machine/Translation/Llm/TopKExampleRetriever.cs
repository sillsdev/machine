using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation.Llm
{
    public abstract class TopKExampleRetriever : ExampleRetriever
    {
        protected TopKExampleRetriever(IEnumerable<Example> examples)
            : base(examples) { }

        /// <summary>One score per example, or an empty array when nothing is indexed.</summary>
        protected abstract double[] ScoreExamples(string query);

        protected override IReadOnlyList<Example> RankCore(string query, int k, int? excludeIndex)
        {
            double[] scores = ScoreExamples(query);
            if (scores.Length == 0)
                return ExampleList.Where((_, i) => i != excludeIndex).Take(k).ToList();

            int available = scores.Length - (excludeIndex.HasValue ? 1 : 0);
            k = Math.Min(k, available);
            if (k <= 0)
                return Array.Empty<Example>();
            if (excludeIndex.HasValue)
                scores[excludeIndex.Value] = double.NegativeInfinity;

            // Highest score first; ties keep corpus order.
            return Enumerable
                .Range(0, scores.Length)
                .OrderByDescending(i => scores[i])
                .ThenBy(i => i)
                .Take(k)
                .Select(i => ExampleList[i])
                .ToList();
        }
    }
}
