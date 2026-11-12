using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Translation.Llm
{
    public abstract class ExampleRetriever
    {
        private readonly List<Example> _examples;

        protected ExampleRetriever(IEnumerable<Example> examples)
        {
            if (examples == null)
                throw new ArgumentNullException(nameof(examples));
            _examples = examples.ToList();
        }

        /// <summary>A copy of the examples this retriever was built from.</summary>
        public IReadOnlyList<Example> Examples => _examples.ToList();

        protected IReadOnlyList<Example> ExampleList => _examples;

        /// <summary>
        /// Builds the retrieval index. Each most-derived constructor must call this once its own state is set up.
        /// </summary>
        protected abstract void Fit();

        /// <summary>
        /// Returns up to <paramref name="k"/> examples ranked for <paramref name="query"/>.
        /// <paramref name="excludeIndex"/> names a position in the example list to leave out; an
        /// out-of-range value excludes nothing.
        /// </summary>
        public IReadOnlyList<Example> Rank(string query, int k, int? excludeIndex = null)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
            if (k <= 0)
                return Array.Empty<Example>();
            if (excludeIndex.HasValue && (excludeIndex.Value < 0 || excludeIndex.Value >= _examples.Count))
                excludeIndex = null;
            return RankCore(query, k, excludeIndex);
        }

        protected abstract IReadOnlyList<Example> RankCore(string query, int k, int? excludeIndex);
    }
}
