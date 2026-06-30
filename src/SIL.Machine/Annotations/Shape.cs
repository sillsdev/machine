using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.ObjectModel;

namespace SIL.Machine.Annotations
{
    /// <summary>
    /// An ordered sequence of <see cref="ShapeNode"/>s plus their annotation tree.
    ///
    /// As of the RUSTIFY flat-shape rework (Phase 3b-impl, Stage 1) a <see cref="Shape"/> owns its nodes
    /// in flat backing arrays addressed by a stable per-node <see cref="ShapeNode.Index"/>: the prev/next
    /// links (an in-array doubly-linked list, so <see cref="AddAfter(ShapeNode, ShapeNode, Direction)"/>
    /// and <see cref="Remove(ShapeNode)"/> stay O(1) and the tag-relabel order maintenance is preserved)
    /// and the per-node frozen flag live here rather than on the node. The list machinery that used to be
    /// inherited from <c>OrderedBidirList&lt;ShapeNode&gt;</c> is reimplemented over those arrays. The
    /// <see cref="ShapeNode"/> objects added to the shape are retained as the canonical one-per-slot
    /// handles, so reference identity is unchanged and behavior is byte-identical.
    /// </summary>
    public class Shape
        : IOrderedBidirList<ShapeNode>,
            IAnnotatedData<ShapeNode>,
            ICloneable<Shape>,
            IFreezable,
            IValueEquatable<Shape>
    {
        // Link sentinel for "no node" (the old null Next/Prev).
        private const int Nil = -1;

        private readonly Func<bool, ShapeNode> _marginSelector;
        private readonly AnnotationList<ShapeNode> _annotations;
        private readonly IEqualityComparer<ShapeNode> _comparer;
        private int _hashCode;

        // Flat backing. Slot 0 = Begin margin, slot 1 = End margin, content nodes from slot 2 up.
        private ShapeNode[] _nodes; // canonical handle per slot (null = free slot)
        private int[] _next; // forward link by slot (Nil = none)
        private int[] _prev; // backward link by slot (Nil = none)
        private bool[] _frozen; // per-node frozen flag by slot
        private int _capacity;
        private int _used; // high-water count of slots ever handed out
        private readonly Stack<int> _free; // reclaimed slots below the high-water mark
        private int _size; // content node count (excludes the two margins)

        private readonly ShapeNode _begin;
        private readonly ShapeNode _end;

        // RUSTIFY Stage 3 (III): copy-on-write clone. A clone of a *frozen* shape stores its source here
        // and does NOT copy the node graph: the hot read path (the FST matcher) consumes the clone only
        // through the int-offset projection (IntAnnotations/IntRange), which is served from the frozen
        // source — so a clone that is only traversed (never has a ShapeNode/Annotation handle handed out
        // and is never mutated) costs a shell, not N nodes + N annotations + their skip-list towers. The
        // first access that needs the real node graph — any flat-backing link read, enumeration, handle
        // bridge (NodeAt), .Annotations access, or mutation — calls EnsureInflated() to materialize it.
        private Shape _cowSource;

        public Shape(Func<bool, ShapeNode> marginSelector)
            : this(marginSelector, new AnnotationList<ShapeNode>()) { }

        public Shape(Func<bool, ShapeNode> marginSelector, AnnotationList<ShapeNode> annotations)
        {
            _marginSelector = marginSelector;
            _annotations = annotations;
            _comparer = EqualityComparer<ShapeNode>.Default;
            _free = new Stack<int>();
            _capacity = 0;
            _used = 0;
            _size = 0;

            _begin = marginSelector(true);
            _end = marginSelector(false);
            Adopt(_begin, AllocSlot());
            Adopt(_end, AllocSlot());
            _next[_begin.Index] = _end.Index;
            _prev[_end.Index] = _begin.Index;

            Begin.Tag = int.MinValue;
            End.Tag = int.MaxValue;
            _annotations.Add(Begin.Annotation, false);
            _annotations.Add(End.Annotation, false);
        }

        protected Shape(Shape shape)
            : this(shape._marginSelector)
        {
            // Copy-on-write only when the source is frozen (immutable, so safe to share): the common
            // case, since words are frozen before being cloned. Flatten any COW chain to the real source.
            if (shape.IsFrozen)
                _cowSource = shape._cowSource ?? shape;
            else
                shape.CopyTo(this);
        }

        // Materialize a copy-on-write clone's real node graph on first access that needs it (see _cowSource).
        // Idempotent and not reentrant: clears _cowSource first, then does the real copy from the (frozen,
        // non-COW) source; re-freezes if this clone had already been frozen-by-sharing.
        private void EnsureInflated()
        {
            if (_cowSource == null)
                return;
            Shape src = _cowSource;
            _cowSource = null;
            bool wasFrozen = IsFrozen;
            if (wasFrozen)
                IsFrozen = false;
            src.CopyTo(this);
            if (wasFrozen)
                Freeze();
        }

        #region Flat backing helpers

        private void EnsureCapacity(int n)
        {
            if (n <= _capacity)
                return;
            int newCap = _capacity == 0 ? 4 : _capacity * 2;
            while (newCap < n)
                newCap *= 2;
            Array.Resize(ref _nodes, newCap);
            Array.Resize(ref _next, newCap);
            Array.Resize(ref _prev, newCap);
            Array.Resize(ref _frozen, newCap);
            _capacity = newCap;
        }

        private int AllocSlot()
        {
            if (_free.Count > 0)
                return _free.Pop();
            int idx = _used++;
            EnsureCapacity(_used);
            return idx;
        }

        private void Adopt(ShapeNode node, int idx)
        {
            _nodes[idx] = node;
            _next[idx] = Nil;
            _prev[idx] = Nil;
            _frozen[idx] = false;
            node.Owner = this;
            node.Index = idx;
        }

        // Detaches a node from this shape (the old OrderedBidirListNode.Clear): frees its slot and
        // resets the handle to the detached state. Does not adjust _size; callers manage that.
        private void Detach(ShapeNode node)
        {
            int idx = node.Index;
            _nodes[idx] = null;
            _next[idx] = Nil;
            _prev[idx] = Nil;
            _frozen[idx] = false;
            node.Owner = null;
            node.Index = -1;
            _free.Push(idx);
        }

        internal ShapeNode GetNextLink(int index)
        {
            if (_cowSource != null)
                EnsureInflated();
            int n = _next[index];
            return n < 0 ? null : _nodes[n];
        }

        internal ShapeNode GetPrevLink(int index)
        {
            if (_cowSource != null)
                EnsureInflated();
            int p = _prev[index];
            return p < 0 ? null : _nodes[p];
        }

        internal bool IsNodeFrozen(int index)
        {
            return _frozen[index];
        }

        internal void SetNodeFrozen(int index)
        {
            _frozen[index] = true;
        }

        #endregion

        public Range<ShapeNode> Range
        {
            get { return Range<ShapeNode>.Create(Begin, End); }
        }

        public AnnotationList<ShapeNode> Annotations
        {
            get
            {
                // Hands out the ShapeNode-keyed annotation tree (morph extraction, rule code, result
                // comparison) — needs the real node graph.
                EnsureInflated();
                return _annotations;
            }
        }

        public bool IsFrozen { get; private set; }

        // ---- RUSTIFY Stage 2: int-offset projection (the Fst<Word,int> bridge) ----
        // The FST binds as Fst<Word,int> with offset = a DENSE per-projection node position (0..N+1 in
        // node order: Begin=0, content 1..N, End=N+1). Dense contiguous offsets — rather than the
        // shape's sparse Tag — are what keep the int model byte-identical: they never collide with the
        // Range<int>.Null = [-1,-1] sentinel, never overflow the half-open +1 (Tag's End == int.MaxValue
        // did), and keep the End anchor a non-empty [N+1, N+2) (matching the ShapeNode anchor's length).
        // These views are rebuilt lazily, gated on the annotation list Version (+ frozen state), so a
        // stable/frozen shape builds them once and reuses them across the thousands of Transduce calls
        // per word, while a shape mutated in place by an iterative rewrite rule rebuilds on next access.
        private AnnotationList<int> _intAnnotations;
        private Dictionary<int, ShapeNode> _byOffset;
        private Dictionary<ShapeNode, int> _nodeOffset;
        private int _intProjectionVersion = -1;
        private bool _intProjectionFrozen;

        public void Freeze()
        {
            if (IsFrozen)
                return;

            // A copy-on-write clone equals its already-frozen source: adopt the frozen state (and its
            // hash) without materializing the node graph, so freeze-then-traverse stays handle-free.
            if (_cowSource != null)
            {
                IsFrozen = true;
                _hashCode = _cowSource.GetFrozenHashCode();
                return;
            }

            IsFrozen = true;
            Begin.Freeze();
            int i = 0;
            foreach (ShapeNode node in this)
            {
                node.Tag = i++;
                node.Freeze();
            }
            End.Freeze();

            _annotations.Freeze();

            _hashCode = 23;
            _hashCode = _hashCode * 31 + Count;
            _hashCode = _hashCode * 31 + _annotations.GetFrozenHashCode();

            // Build the int-offset projection now, while frozen and single-threaded. A frozen shape is
            // immutable, so this projection is final — and (RUSTIFY Stage 3 / COW) copy-on-write clones
            // delegate their IntAnnotations to this frozen source, possibly from several parse threads at
            // once. Building eagerly here means those concurrent reads always hit a complete cache rather
            // than racing a lazy first build of the offset dictionaries. No extra work overall: a frozen
            // shape that is frozen is one that will be traversed (by itself or its COW clones).
            EnsureIntProjection();
        }

        // Maps a ShapeNode annotation range to its int-offset range using the dense per-projection node
        // positions: a single node [n, n] -> half-open [off(n), off(n)+1); a span [s, e] ->
        // [off(s), off(e)+1). Relationship-preserving vs the inclusive ShapeNode form (see the
        // IntOffsetRangeMapping parity test); dense offsets make it free of the Tag edge cases.
        private Range<int> ToIntRange(Range<ShapeNode> r)
        {
            return Range<int>.Create(_nodeOffset[r.Start], _nodeOffset[r.End] + 1);
        }

        private void EnsureIntProjection()
        {
            if (
                _intAnnotations != null
                && _intProjectionVersion == _annotations.Version
                && _intProjectionFrozen == IsFrozen
            )
            {
                return;
            }

            // Assign dense offsets to every node in node order: Begin=0, content 1..N, End=N+1.
            _nodeOffset = new Dictionary<ShapeNode, int>();
            _byOffset = new Dictionary<int, ShapeNode>();
            int pos = 0;
            AssignOffset(Begin, ref pos);
            foreach (ShapeNode node in this)
                AssignOffset(node, ref pos);
            AssignOffset(End, ref pos);

            var dest = new AnnotationList<int>();
            foreach (Annotation<ShapeNode> top in _annotations)
                dest.Add(ProjectAnnotation(top), false);

            _intAnnotations = dest;
            _intProjectionVersion = _annotations.Version;
            _intProjectionFrozen = IsFrozen;
        }

        private void AssignOffset(ShapeNode node, ref int pos)
        {
            _nodeOffset[node] = pos;
            _byOffset[pos] = node;
            pos++;
        }

        private Annotation<int> ProjectAnnotation(Annotation<ShapeNode> src)
        {
            // Share the FeatureStruct by reference (no clone): the int annotation is a view, and a
            // rule's in-place FeatureStruct edit on a matched node must remain visible.
            var ann = new Annotation<int>(ToIntRange(src.Range), src.FeatureStruct) { Optional = src.Optional };
            if (!src.IsLeaf)
            {
                foreach (Annotation<ShapeNode> child in src.Children)
                    ann.Children.Add(ProjectAnnotation(child), false);
            }
            return ann;
        }

        /// <summary>
        /// The int-offset projection of this shape's annotations (RUSTIFY Stage 2): the
        /// <see cref="AnnotationList{T}"/> the <c>Fst&lt;Word,int&gt;</c> traversal consumes. Built
        /// lazily and cached against the annotation <see cref="AnnotationList{T}.Version"/>.
        /// </summary>
        public AnnotationList<int> IntAnnotations
        {
            get
            {
                // The whole point of COW: serve the projection from the frozen source without
                // materializing this clone's node graph. This is the FST matcher's only access path.
                if (_cowSource != null)
                    return _cowSource.IntAnnotations;
                EnsureIntProjection();
                return _intAnnotations;
            }
        }

        /// <summary>
        /// The whole-shape int range — the half-open image of the inclusive ShapeNode range
        /// <c>[Begin, End]</c>, i.e. <c>[off(Begin), off(End) + 1)</c>. The <c>+1</c> matters: the only
        /// framework consumer is <c>Matcher.GetStartAnnotation</c> via <c>Range.GetStart(dir)</c>, and a
        /// right-to-left match starts at <c>GetStart(RtL) == End</c>. The End anchor's dense node range
        /// is <c>[off(End), off(End)+1)</c>, whose RtL start coordinate is <c>off(End)+1</c> — so End
        /// must be <c>off(End)+1</c> for a RtL match to begin <em>at</em> the End anchor rather than at
        /// the last content node (which would skip any edit adjacent to End, e.g. inserting a deleted
        /// segment after the final vowel during analysis).
        /// </summary>
        public Range<int> IntRange
        {
            get
            {
                if (_cowSource != null)
                    return _cowSource.IntRange;
                EnsureIntProjection();
                return Range<int>.Create(_nodeOffset[Begin], _nodeOffset[End] + 1);
            }
        }

        /// <summary>
        /// Resolves an int offset (a dense node position) back to its node — the reverse of the
        /// int-offset projection, used by rule RHS code to act on the segment graph. Works on frozen
        /// and unfrozen shapes.
        /// </summary>
        public ShapeNode NodeAt(int offset)
        {
            // Hands out a real ShapeNode of this shape (rule-RHS / mutation path) — must materialize.
            EnsureInflated();
            EnsureIntProjection();
            return _byOffset[offset];
        }

        /// <summary>
        /// The int offset (dense node position) of a node. Companion to <see cref="NodeAt"/>.
        /// </summary>
        public int OffsetOf(ShapeNode node)
        {
            EnsureInflated();
            EnsureIntProjection();
            return _nodeOffset[node];
        }

        /// <summary>
        /// The offset to pass to <c>Matcher.Match(input, start)</c> to begin matching <em>at</em>
        /// <paramref name="node"/> in direction <paramref name="dir"/>. A node's half-open annotation
        /// is <c>[off, off+1)</c>, and the matcher locates the start annotation by its
        /// <c>Range.GetStart(dir)</c>: that is <c>off</c> left-to-right but <c>off+1</c> right-to-left.
        /// (With the old inclusive <c>[node, node]</c> ShapeNode ranges this was direction-agnostic;
        /// the dense half-open int model needs this adjustment to stay byte-identical for RtL matches.)
        /// </summary>
        public int MatchStartOffset(ShapeNode node, Direction dir)
        {
            EnsureInflated();
            EnsureIntProjection();
            int off = _nodeOffset[node];
            return dir == Direction.LeftToRight ? off : off + 1;
        }

        private void CheckFrozen()
        {
            if (IsFrozen)
                throw new InvalidOperationException("The shape is immutable.");
        }

        #region ICollection / IBidirList

        public int Count
        {
            // COW-safe: the clone's content count equals its frozen source's, without inflating.
            get { return _cowSource != null ? _cowSource.Count : _size; }
        }

        bool ICollection<ShapeNode>.IsReadOnly
        {
            get { return false; }
        }

        public ShapeNode Begin
        {
            get { return _begin; }
        }

        public ShapeNode End
        {
            get { return _end; }
        }

        public ShapeNode GetBegin(Direction dir)
        {
            return dir == Direction.LeftToRight ? Begin : End;
        }

        public ShapeNode GetEnd(Direction dir)
        {
            return dir == Direction.LeftToRight ? End : Begin;
        }

        public ShapeNode First
        {
            // Count is COW-aware; GetNextLink inflates if needed, so this hands out a real node.
            get { return Count == 0 ? null : GetNextLink(_begin.Index); }
        }

        public ShapeNode Last
        {
            get { return Count == 0 ? null : GetPrevLink(_end.Index); }
        }

        public ShapeNode GetFirst(Direction dir)
        {
            return dir == Direction.LeftToRight ? First : Last;
        }

        public ShapeNode GetLast(Direction dir)
        {
            return dir == Direction.LeftToRight ? Last : First;
        }

        public ShapeNode GetNext(ShapeNode cur)
        {
            return GetNext(cur, Direction.LeftToRight);
        }

        public ShapeNode GetNext(ShapeNode cur, Direction dir)
        {
            if (cur.List != this)
                throw new ArgumentException("cur is not a member of this collection.", "cur");
            return dir == Direction.LeftToRight ? cur.Next : cur.Prev;
        }

        public ShapeNode GetPrev(ShapeNode cur)
        {
            return GetPrev(cur, Direction.LeftToRight);
        }

        public ShapeNode GetPrev(ShapeNode cur, Direction dir)
        {
            if (cur.List != this)
                throw new ArgumentException("cur is not a member of this collection.", "cur");
            return dir == Direction.LeftToRight ? cur.Prev : cur.Next;
        }

        public bool Find(ShapeNode example, out ShapeNode result)
        {
            return Find(example, Direction.LeftToRight, out result);
        }

        public bool Find(ShapeNode start, ShapeNode example, out ShapeNode result)
        {
            return Find(start, example, Direction.LeftToRight, out result);
        }

        public bool Find(ShapeNode example, Direction dir, out ShapeNode result)
        {
            return Find(GetFirst(dir), example, dir, out result);
        }

        public bool Find(ShapeNode start, ShapeNode example, Direction dir, out ShapeNode result)
        {
            for (ShapeNode n = start; n != GetEnd(dir); n = n.GetNext(dir))
            {
                if (_comparer.Equals(example, n))
                {
                    result = n;
                    return true;
                }
            }
            result = null;
            return false;
        }

        public bool Contains(ShapeNode node)
        {
            return node.List == this;
        }

        public void CopyTo(ShapeNode[] array, int arrayIndex)
        {
            foreach (ShapeNode node in this)
                array[arrayIndex++] = node;
        }

        IEnumerator<ShapeNode> IEnumerable<ShapeNode>.GetEnumerator()
        {
            // Count is COW-aware; First inflates if needed. (Use Count, not _size — a COW clone has
            // _size == 0 until inflated.)
            if (Count == 0)
                yield break;

            for (ShapeNode node = First; node != End; node = node.Next)
                yield return node;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ShapeNode>)this).GetEnumerator();
        }

        public void Add(ShapeNode node)
        {
            AddAfter(_end.Prev, node, Direction.LeftToRight);
        }

        public void AddRange(IEnumerable<ShapeNode> e)
        {
            foreach (ShapeNode node in e)
                Add(node);
        }

        public void AddRangeAfter(ShapeNode node, IEnumerable<ShapeNode> newNodes, Direction dir)
        {
            if (_size == 0 && node == null)
                node = GetBegin(dir);

            if (node.List != this)
                throw new ArgumentException("node is not a member of this collection.", "node");

            foreach (ShapeNode newNode in newNodes)
            {
                AddAfter(node, newNode, dir);
                node = newNode;
            }
        }

        public void AddRangeAfter(ShapeNode node, IEnumerable<ShapeNode> newNodes)
        {
            AddRangeAfter(node, newNodes, Direction.LeftToRight);
        }

        public void AddAfter(ShapeNode node, ShapeNode newNode)
        {
            AddAfter(node, newNode, Direction.LeftToRight);
        }

        #endregion

        public ShapeNode Add(FeatureStruct fs)
        {
            return Add(fs, false);
        }

        public ShapeNode Add(FeatureStruct fs, bool optional)
        {
            var newNode = new ShapeNode(fs);
            newNode.Annotation.Optional = optional;
            Add(newNode);
            return newNode;
        }

        public Range<ShapeNode> CopyTo(Shape dest)
        {
            if (Count == 0)
                return Range<ShapeNode>.Null;
            return CopyTo(First, Last, dest);
        }

        public Range<ShapeNode> CopyTo(ShapeNode srcStart, ShapeNode srcEnd, Shape dest)
        {
            return CopyTo(Range<ShapeNode>.Create(srcStart, srcEnd), dest);
        }

        // Per-thread scratch map reused across CopyTo calls. CopyTo runs on every Word.Clone
        // (hundreds per parse on a real grammar) and the map is fully consumed before CopyTo
        // returns (never retained) and CopyTo is not reentrant, so reusing one map per thread
        // removes a per-clone Dictionary allocation without any sharing hazard. This is a SAFE
        // pool — unlike the across-word FST arena (RUSTIFY Phase 1b), nothing here survives the
        // call, so it cannot promote parse data to Gen2 / regress parallel parsing.
        [ThreadStatic]
        private static Dictionary<ShapeNode, ShapeNode> CloneMapping;

        public Range<ShapeNode> CopyTo(Range<ShapeNode> srcRange, Shape dest)
        {
            // Reads this shape's real node graph + annotations as the copy source — materialize if COW.
            // (When called from EnsureInflated the source is the real frozen shape, so this is a no-op.)
            EnsureInflated();
            ShapeNode startNode = null;
            ShapeNode endNode = null;
            // Build the src->dest node mapping inline while cloning, instead of a second pass
            // with GetNodes().Zip().ToDictionary(). CopyTo runs on every Word.Clone (thousands
            // per parse on a real grammar), so eliminating the extra enumerations + LINQ
            // allocations per clone is a measurable GC win.
            Dictionary<ShapeNode, ShapeNode> mapping = CloneMapping;
            if (mapping == null)
                mapping = CloneMapping = new Dictionary<ShapeNode, ShapeNode>();
            mapping.Clear();
            foreach (ShapeNode node in GetNodes(srcRange))
            {
                ShapeNode newNode = node.Clone();
                if (startNode == null)
                    startNode = newNode;
                endNode = newNode;
                dest.Add(newNode);
                mapping[node] = newNode;
            }

            Range<ShapeNode> destRange = Range<ShapeNode>.Create(startNode, endNode);
            foreach (Annotation<ShapeNode> ann in _annotations.GetNodes(srcRange))
                CopyAnnotations(dest._annotations, ann, mapping);

            return destRange;
        }

        private void CopyAnnotations(
            AnnotationList<ShapeNode> destList,
            Annotation<ShapeNode> ann,
            Dictionary<ShapeNode, ShapeNode> mapping
        )
        {
            if (ann.Range.Start.Annotation == ann)
            {
                destList.Add(mapping[ann.Range.Start].Annotation, false);
            }
            else
            {
                var newAnn = new Annotation<ShapeNode>(
                    Range<ShapeNode>.Create(mapping[ann.Range.Start], mapping[ann.Range.End]),
                    ann.FeatureStruct.Clone()
                );
                destList.Add(newAnn, false);
                if (!ann.IsLeaf)
                {
                    foreach (Annotation<ShapeNode> child in ann.Children)
                        CopyAnnotations(newAnn.Children, child, mapping);
                }
            }
        }

        public ShapeNode AddAfter(ShapeNode node, FeatureStruct fs)
        {
            return AddAfter(node, fs, Direction.LeftToRight);
        }

        public ShapeNode AddAfter(ShapeNode node, FeatureStruct fs, bool optional)
        {
            return AddAfter(node, fs, optional, Direction.LeftToRight);
        }

        public ShapeNode AddAfter(ShapeNode node, FeatureStruct fs, Direction dir)
        {
            return AddAfter(node, fs, false, dir);
        }

        public ShapeNode AddAfter(ShapeNode node, FeatureStruct fs, bool optional, Direction dir)
        {
            var newNode = new ShapeNode(fs);
            newNode.Annotation.Optional = optional;
            AddAfter(node, newNode, dir);
            return newNode;
        }

        public void AddAfter(ShapeNode node, ShapeNode newNode, Direction dir)
        {
            CheckFrozen();
            EnsureInflated();
            if (newNode.List == this)
                throw new ArgumentException("newNode is already a member of this collection.", "newNode");
            if (node != null && node.List != this)
                throw new ArgumentException("node is not a member of this collection.", "node");

            if (Count == 0)
            {
                newNode.Tag = 0;
            }
            else
            {
                ShapeNode curNode = node;
                if (dir == Direction.RightToLeft)
                    curNode = curNode == null ? Last : curNode.Prev;

                if (curNode == null)
                {
                    if (First.Tag == int.MinValue + 1)
                        RelabelMinimumSparseEnclosingRange(null);
                }
                else if (curNode.Next == null)
                {
                    if (curNode.Tag == int.MaxValue - 1)
                        RelabelMinimumSparseEnclosingRange(curNode);
                }
                else if (curNode.Tag + 1 == curNode.Next.Tag)
                {
                    RelabelMinimumSparseEnclosingRange(curNode);
                }

                if (curNode != null && curNode.Next == null)
                {
                    newNode.Tag = Average(curNode.Tag, int.MaxValue);
                }
                else
                {
                    newNode.Tag = Average(
                        curNode == null ? int.MinValue : curNode.Tag,
                        curNode == null ? First.Tag : curNode.Next.Tag
                    );
                }
            }

            // Splice newNode into the in-array linked list (was OrderedBidirList.AddAfter).
            if (Count == 0 && node == null)
                node = GetBegin(dir);

            newNode.Remove();
            Adopt(newNode, AllocSlot());

            ShapeNode anchor = node;
            if (dir == Direction.RightToLeft)
                anchor = anchor.Prev;

            int aIdx = anchor.Index;
            int sIdx = newNode.Index;
            int afterIdx = _next[aIdx];
            _next[sIdx] = afterIdx;
            _next[aIdx] = sIdx;
            _prev[sIdx] = aIdx;
            if (afterIdx >= 0)
                _prev[afterIdx] = sIdx;

            _size++;

            _annotations.Add(newNode.Annotation);
        }

        public bool Remove(ShapeNode node)
        {
            CheckFrozen();
            EnsureInflated();
            if (node.List != this)
                return false;

            node.Annotation.Remove();
            UpdateAnnotations(_annotations, node);

            int idx = node.Index;
            int p = _prev[idx];
            int n = _next[idx];
            if (p >= 0)
                _next[p] = n;
            if (n >= 0)
                _prev[n] = p;
            Detach(node);
            _size--;
            return true;
        }

        private void UpdateAnnotations(AnnotationList<ShapeNode> annList, ShapeNode node)
        {
            if (annList.Count == 0)
                return;

            Annotation<ShapeNode> startAnn;
            annList.Find(node, Direction.LeftToRight, out startAnn);
            if (startAnn == annList.Begin)
                startAnn = annList.First;

            Annotation<ShapeNode> endAnn;
            annList.Find(node, Direction.RightToLeft, out endAnn);
            if (endAnn == annList.End)
                endAnn = annList.Last;

            if (startAnn.CompareTo(endAnn) > 0)
                return;

            foreach (
                Annotation<ShapeNode> ann in annList
                    .GetNodes(startAnn, endAnn)
                    .Where(ann => ann.Range.Contains(node))
                    .ToArray()
            )
            {
                if (!ann.IsLeaf)
                    UpdateAnnotations(ann.Children, node);

                if (ann.Range.Start == node && ann.Range.End == node)
                {
                    annList.Remove(ann);
                }
                else if (ann.Range.Start == node || ann.Range.End == node)
                {
                    Range<ShapeNode> range =
                        ann.Range.Start == node
                            ? Range<ShapeNode>.Create(node.Next, ann.Range.End)
                            : Range<ShapeNode>.Create(ann.Range.Start, node.Prev);
                    var newAnn = new Annotation<ShapeNode>(range, ann.FeatureStruct.Clone())
                    {
                        Optional = ann.Optional,
                    };
                    if (!ann.IsLeaf)
                    {
                        foreach (Annotation<ShapeNode> child in ann.Children.ToArray())
                            newAnn.Children.Add(child, false);
                    }
                    annList.Remove(ann, false);
                    annList.Add(newAnn, false);
                }
            }
        }

        public void Clear()
        {
            CheckFrozen();
            EnsureInflated();
            foreach (ShapeNode node in this.ToArray())
                Detach(node);
            _next[_begin.Index] = _end.Index;
            _prev[_end.Index] = _begin.Index;
            _size = 0;
            _annotations.Clear();
            _annotations.Add(Begin.Annotation);
            _annotations.Add(End.Annotation);
        }

        private static int Average(int x, int y)
        {
            return (x & y) + (x ^ y) / 2;
        }

        private const int NumBits = (sizeof(int) * 8) - 2;

        private void RelabelMinimumSparseEnclosingRange(ShapeNode node)
        {
            double t = Math.Pow(Math.Pow(2, NumBits) / Count, 1.0 / NumBits);

            double elementCount = 1.0;

            ShapeNode left = node;
            ShapeNode right = node;
            int tag = node == null ? int.MinValue : node.Tag;
            int low = tag;
            int high = tag;

            int level = 0;
            double overflowThreshold = 1.0;
            int range = 1;
            do
            {
                int toggleBit = 1 << level++;
                overflowThreshold /= t;
                range <<= 1;

                bool expandToLeft = (tag & toggleBit) != 0;
                if (expandToLeft)
                {
                    low ^= toggleBit;
                    while (left != null && left.Tag > low)
                    {
                        left = left.Prev;
                        elementCount++;
                    }
                }
                else
                {
                    high ^= toggleBit;
                    while (right == null || (right.Tag < high && (right.Next != null && right.Next.Tag > right.Tag)))
                    {
                        right = right == null ? First : right.Next;
                        elementCount++;
                    }
                }
            } while (elementCount >= (range * overflowThreshold) && level < NumBits);

            var count = (int)elementCount; //elementCount always fits into an int, size() is an int too

            //note that the base itself can be relabeled, but always gets the same label! (int.MIN_VALUE)
            int pos = low;
            int step = range / count;
            ShapeNode cursor = left;
            if (step > 1)
            {
                for (int i = 0; i < count; i++)
                {
                    if (cursor != null)
                        cursor.Tag = pos;
                    pos += step;
                    cursor = cursor == null ? First : cursor.Next;
                }
            }
            else
            { //handle degenerate case here (step == 1)
                //make sure that this and next are separated by distance of at least 2
                int slack = range - count;
                for (int i = 0; i < elementCount; i++)
                {
                    if (cursor != null)
                        cursor.Tag = pos;
                    pos++;
                    if (node == cursor)
                        pos += slack;
                    cursor = cursor == null ? First : cursor.Next;
                }
            }
        }

        public IEnumerable<ShapeNode> GetNodes(Range<ShapeNode> range)
        {
            return GetNodes(range, Direction.LeftToRight);
        }

        public IEnumerable<ShapeNode> GetNodes(Range<ShapeNode> range, Direction dir)
        {
            EnsureInflated();
            return this.GetNodes(range.GetStart(dir), range.GetEnd(dir), dir);
        }

        public bool ValueEquals(Shape other)
        {
            if (Count != other.Count)
                return false;

            EnsureInflated();
            // other.Annotations (the getter) inflates other if it is COW.
            return _annotations.ValueEquals(other.Annotations);
        }

        public int GetFrozenHashCode()
        {
            if (!IsFrozen)
            {
                throw new InvalidOperationException(
                    "The shape does not have a valid hash code, because it is mutable."
                );
            }

            return _hashCode;
        }

        public Shape Clone()
        {
            return new Shape(this);
        }
    }
}
