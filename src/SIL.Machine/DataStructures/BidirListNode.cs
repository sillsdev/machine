namespace SIL.Machine.DataStructures
{
    public abstract class BidirListNode<TNode> : IBidirListNode<TNode>
        where TNode : BidirListNode<TNode>
    {
        // Skip-list tower links. Level 0 (the only level ~50% of nodes have) is stored inline in fields so
        // those nodes allocate no tower array at all, and every taller node's array is one slot shorter;
        // levels 1.. live in _nextHigh/_prevHigh (null when Levels <= 1). The per-node `new TNode[levels]`
        // towers were the dominant Word.Clone sub-cost on Sena (RUSTIFY Stage 3, increment II).
        private BidirList<TNode> _list;
        private TNode _next0;
        private TNode _prev0;
        private TNode[] _nextHigh;
        private TNode[] _prevHigh;

        public IBidirList<TNode> List
        {
            get { return _list; }
        }

        public TNode Next
        {
            get { return Levels == 0 ? null : _next0; }
        }

        public TNode Prev
        {
            get { return Levels == 0 ? null : _prev0; }
        }

        /// <summary>
        /// Gets the next node in the owning linked list according to the
        /// specified direction.
        /// </summary>
        /// <param name="dir">The direction</param>
        /// <returns>The next node.</returns>
        public TNode GetNext(Direction dir)
        {
            if (List == null)
                return null;

            return List.GetNext((TNode)this, dir);
        }

        /// <summary>
        /// Gets the previous node in the owning linked list according to the
        /// specified direction.
        /// </summary>
        /// <param name="dir">The direction</param>
        /// <returns>The previous node.</returns>
        public TNode GetPrev(Direction dir)
        {
            if (List == null)
                return null;

            return List.GetPrev((TNode)this, dir);
        }

        public bool Remove()
        {
            if (List == null)
                return false;

            return List.Remove((TNode)this);
        }

        protected internal virtual void Init(BidirList<TNode> list, int levels)
        {
            _list = list;
            _next0 = null;
            _prev0 = null;
            _nextHigh = levels > 1 ? new TNode[levels - 1] : null;
            _prevHigh = levels > 1 ? new TNode[levels - 1] : null;
            Levels = levels;
        }

        // Grow this node's high-level tower arrays to hold a list of total height `levels`. Used by
        // BidirList for the Begin/End margins, which grow as the skip list gets taller instead of being
        // pre-allocated to the 33-level maximum up front (most skip lists stay shallow). Right-sizes the
        // exact level: margins grow one level at a time and the shallow majority never reach here, so
        // geometric growth would only over-allocate; the O(height^2) churn it avoids is bounded by the
        // ~31-level skip-list cap and only reached by rare very large lists.
        internal void EnsureLevelCapacity(int levels)
        {
            int needHigh = levels - 1;
            if (needHigh <= 0 || (_nextHigh?.Length ?? 0) >= needHigh)
                return;
            System.Array.Resize(ref _nextHigh, needHigh);
            System.Array.Resize(ref _prevHigh, needHigh);
        }

        protected internal virtual void Clear()
        {
            _list = null;
            _next0 = null;
            _prev0 = null;
            _nextHigh = null;
            _prevHigh = null;
            Levels = 0;
        }

        internal int Levels { get; set; }

        internal TNode GetNext(int level)
        {
            return level == 0 ? _next0 : _nextHigh[level - 1];
        }

        internal void SetNext(int level, TNode node)
        {
            if (level == 0)
                _next0 = node;
            else
                _nextHigh[level - 1] = node;
        }

        internal TNode GetPrev(int level)
        {
            return level == 0 ? _prev0 : _prevHigh[level - 1];
        }

        internal void SetPrev(int level, TNode node)
        {
            if (level == 0)
                _prev0 = node;
            else
                _prevHigh[level - 1] = node;
        }
    }
}
