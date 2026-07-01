using System.Collections.Generic;
using SIL.Extensions;
using SIL.Machine.Annotations;

namespace SIL.Machine.FiniteState
{
    internal class NondeterministicFstTraversalInstance<TData, TOffset> : TraversalInstance<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        // RUSTIFY lever 1: value-type bitset over state indices instead of a HashSet<State> (no
        // per-instance set allocation).
        private VisitedStates _visited;
        private readonly Dictionary<Annotation<TOffset>, Annotation<TOffset>> _mappings;
        private readonly List<Output<TData, TOffset>> _outputs;

        public NondeterministicFstTraversalInstance(int registerCount)
            : base(registerCount, false)
        {
            _mappings = new Dictionary<Annotation<TOffset>, Annotation<TOffset>>();
            _outputs = new List<Output<TData, TOffset>>();
        }

        public bool IsVisited(State<TData, TOffset> state)
        {
            return _visited.Contains(state.Index);
        }

        public void MarkVisited(State<TData, TOffset> state)
        {
            _visited.Add(state.Index);
        }

        public void ClearVisited()
        {
            _visited.Clear();
        }

        public IDictionary<Annotation<TOffset>, Annotation<TOffset>> Mappings
        {
            get { return _mappings; }
        }

        public IList<Output<TData, TOffset>> Outputs
        {
            get { return _outputs; }
        }

        public override void CopyTo(TraversalInstance<TData, TOffset> other)
        {
            base.CopyTo(other);

            var otherNfst = (NondeterministicFstTraversalInstance<TData, TOffset>)other;

            otherNfst._visited.UnionWith(in _visited);
            // The original built `outputMappings` by zipping this.Output's node sequence with itself
            // — a deterministic (Queue-based BFS) enumeration paired element-for-element, i.e. the
            // identity map — so `outputMappings[v] == v` and the whole block reduces to copying
            // _mappings unchanged. Doing that directly avoids a Dictionary + two SelectMany(BFS,
            // each allocating a Queue + iterator) + Zip + Select per instance copy (very hot in
            // nondeterministic traversal). Byte-identical; otherNfst._mappings is empty here
            // (GetCachedInstance -> Clear()).
            otherNfst._mappings.AddRange(_mappings);
            otherNfst._outputs.AddRange(_outputs);
        }

        public override void Clear()
        {
            base.Clear();
            _visited.Clear();
            _mappings.Clear();
            _outputs.Clear();
        }
    }
}
