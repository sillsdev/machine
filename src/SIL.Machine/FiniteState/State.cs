using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.ObjectModel;

namespace SIL.Machine.FiniteState
{
    public class State<TData, TOffset> : IFreezable
        where TData : IAnnotatedData<TOffset>
    {
        private readonly int _index;
        private readonly ArcCollection<TData, TOffset> _arcs;

        private readonly FreezableList<AcceptInfo<TData, TOffset>> _acceptInfos;
        private readonly List<TagMapCommand> _finishers;
        private readonly bool _isLazy;
        private bool _isAccepting;

        internal State(bool isFsa, int index, bool isAccepting)
            : this(
                isFsa,
                index,
                isAccepting,
                Enumerable.Empty<AcceptInfo<TData, TOffset>>(),
                Enumerable.Empty<TagMapCommand>(),
                false
            ) { }

        internal State(bool isFsa, int index, IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos)
            : this(isFsa, index, true, acceptInfos, Enumerable.Empty<TagMapCommand>(), false) { }

        internal State(
            bool isFsa,
            int index,
            IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos,
            IEnumerable<TagMapCommand> finishers,
            bool isLazy
        )
            : this(isFsa, index, true, acceptInfos, finishers, isLazy) { }

        private State(
            bool isFsa,
            int index,
            bool isAccepting,
            IEnumerable<AcceptInfo<TData, TOffset>> acceptInfos,
            IEnumerable<TagMapCommand> finishers,
            bool isLazy
        )
        {
            _index = index;
            IsAccepting = isAccepting;
            _acceptInfos = new FreezableList<AcceptInfo<TData, TOffset>>(acceptInfos);
            _finishers = new List<TagMapCommand>(finishers);
            _isLazy = isLazy;
            _arcs = new ArcCollection<TData, TOffset>(isFsa, this);
        }

        public int Index
        {
            get { return _index; }
        }

        public bool IsAccepting
        {
            get { return _isAccepting; }
            set
            {
                CheckFrozen();
                _isAccepting = value;
            }
        }

        public ArcCollection<TData, TOffset> Arcs
        {
            get { return _arcs; }
        }

        public IList<AcceptInfo<TData, TOffset>> AcceptInfos
        {
            get { return _acceptInfos; }
        }

        public bool IsLazy
        {
            get { return _isLazy; }
        }

        internal List<TagMapCommand> Finishers
        {
            get { return _finishers; }
        }

        public override string ToString()
        {
            return string.Format("State {0}", _index);
        }

        // Without this override, GetHashCode() falls back to the CLR's default identity hash
        // (RuntimeHelpers.GetHashCode's sync-block-index path) — a CPU profile showed that call
        // dominating self-time on the hot nondeterministic-traversal dedup path (TraversalKey's
        // hash folds in State.GetHashCode() once per pushed instance). _index is a stable,
        // already-unique-per-Fst int assigned once at construction, so it is a valid, far cheaper
        // hash; Equals() is intentionally left as reference equality (state objects are singletons
        // within their Fst, never recreated), so the Equals/GetHashCode contract still holds.
        public override int GetHashCode()
        {
            return _index;
        }

        private void CheckFrozen()
        {
            if (IsFrozen)
                throw new InvalidOperationException("The FST is immutable.");
        }

        public bool IsFrozen { get; private set; }

        public void Freeze()
        {
            if (IsFrozen)
                return;

            IsFrozen = true;
            _arcs.Freeze();
            _acceptInfos.Freeze();
        }

        public int GetFrozenHashCode()
        {
            return GetHashCode();
        }
    }
}
