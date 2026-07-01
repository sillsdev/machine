using SIL.Machine.Annotations;

namespace SIL.Machine.FiniteState
{
    internal class NondeterministicFsaTraversalInstance<TData, TOffset> : TraversalInstance<TData, TOffset>
        where TData : IAnnotatedData<TOffset>
    {
        // RUSTIFY lever 1: a value-type bitset over state indices instead of a HashSet<State> — no
        // per-instance set allocation (the instance is created ~2,927x/word on Sena).
        private VisitedStates _visited;

        public NondeterministicFsaTraversalInstance(int registerCount)
            : base(registerCount, false) { }

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

        public override void CopyTo(TraversalInstance<TData, TOffset> other)
        {
            base.CopyTo(other);
            var otherNfsa = (NondeterministicFsaTraversalInstance<TData, TOffset>)other;
            otherNfsa._visited.UnionWith(in _visited);
        }

        public override void Clear()
        {
            base.Clear();
            _visited.Clear();
        }
    }
}
