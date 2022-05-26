using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.DataStructures
{
    /// <summary>
    /// This is a bi-directional list. It is optimized for list traversal in either direction.
    /// </summary>
    /// <typeparam name="TNode">Item Type, must be the type of the class that the linked list handles.</typeparam>
    public abstract class OrderedBidirList<TNode> : IOrderedBidirList<TNode> where TNode : OrderedBidirListNode<TNode>
    {
        private readonly TNode _begin;
        private readonly TNode _end;
        private int _size;

        private readonly IEqualityComparer<TNode> _comparer;

        protected OrderedBidirList(IEqualityComparer<TNode> comparer, Func<bool, TNode> marginSelector)
        {
            _comparer = comparer;
            _begin = marginSelector(true);
            _end = marginSelector(false);
            _begin.Init(this);
            _begin.Next = _end;
            _end.Init(this);
            _end.Prev = _begin;
        }

        public int Count
        {
            get { return _size; }
        }

        bool ICollection<TNode>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Adds the specified node to the end of this list.
        /// </summary>
        /// <param name="node">The node.</param>
        public void Add(TNode node)
        {
            AddAfter(_end.Prev, node, Direction.LeftToRight);
        }

        public virtual void Clear()
        {
            foreach (TNode node in this.ToArray())
                node.Clear();
            _begin.Next = _end;
            _end.Prev = _begin;
            _size = 0;
        }

        public bool Contains(TNode node)
        {
            return node.List == this;
        }

        public void CopyTo(TNode[] array, int arrayIndex)
        {
            foreach (TNode node in this)
                array[arrayIndex++] = node;
        }

        /// <summary>
        /// Removes the specified node from this list.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns><c>true</c> if <c>node</c> is a member of this list, otherwise <c>false</c></returns>
        public virtual bool Remove(TNode node)
        {
            if (node.List != this)
                return false;

            node.Prev.Next = node.Next;
            node.Next.Prev = node.Prev;

            node.Clear();

            _size--;

            return true;
        }

        IEnumerator<TNode> IEnumerable<TNode>.GetEnumerator()
        {
            if (_size == 0)
                yield break;

            for (TNode node = First; node != End; node = node.Next)
                yield return node;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TNode>)this).GetEnumerator();
        }

        public TNode Begin
        {
            get { return _begin; }
        }

        public TNode End
        {
            get { return _end; }
        }

        public TNode GetBegin(Direction dir)
        {
            if (dir == Direction.LeftToRight)
                return Begin;

            return End;
        }

        public TNode GetEnd(Direction dir)
        {
            if (dir == Direction.LeftToRight)
                return End;

            return Begin;
        }

        /// <summary>
        /// Gets the first node in this list.
        /// </summary>
        /// <value>The first node.</value>
        public TNode First
        {
            get
            {
                if (_size == 0)
                    return null;
                return _begin.Next;
            }
        }

        /// <summary>
        /// Gets the last node in this list.
        /// </summary>
        /// <value>The last node.</value>
        public TNode Last
        {
            get
            {
                if (_size == 0)
                    return null;
                return _end.Prev;
            }
        }

        /// <summary>
        /// Gets the first node in this list according to the specified direction.
        /// </summary>
        /// <param name="dir">The direction.</param>
        /// <returns>The first node.</returns>
        public TNode GetFirst(Direction dir)
        {
            if (dir == Direction.LeftToRight)
                return First;

            return Last;
        }

        /// <summary>
        /// Gets the last node in this list according to the specified direction.
        /// </summary>
        /// <param name="dir">The direction.</param>
        /// <returns>The last node.</returns>
        public TNode GetLast(Direction dir)
        {
            if (dir == Direction.LeftToRight)
                return Last;

            return First;
        }

        /// <summary>
        /// Gets the node after the specified node.
        /// </summary>
        /// <param name="cur">The current node.</param>
        /// <returns>The next node.</returns>
        public TNode GetNext(TNode cur)
        {
            return GetNext(cur, Direction.LeftToRight);
        }

        /// <summary>
        /// Gets the node after the specified node according to the specified direction.
        /// </summary>
        /// <param name="cur">The current node.</param>
        /// <param name="dir">The direction.</param>
        /// <returns>The next node.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the specified node is not owned by this linked list.</exception>
        public TNode GetNext(TNode cur, Direction dir)
        {
            if (cur.List != this)
                throw new ArgumentException("cur is not a member of this collection.", "cur");

            if (dir == Direction.LeftToRight)
                return cur.Next;

            return cur.Prev;
        }

        /// <summary>
        /// Gets the node before the specified node.
        /// </summary>
        /// <param name="cur">The current node.</param>
        /// <returns>The previous node.</returns>
        public TNode GetPrev(TNode cur)
        {
            return GetPrev(cur, Direction.LeftToRight);
        }

        /// <summary>
        /// Gets the node before the specified node according to the specified direction.
        /// </summary>
        /// <param name="cur">The current node.</param>
        /// <param name="dir">The direction.</param>
        /// <returns>The previous node.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the specified node is not owned by this linked list.</exception>
        public TNode GetPrev(TNode cur, Direction dir)
        {
            if (cur.List != this)
                throw new ArgumentException("cur is not a member of this collection.", "cur");

            if (dir == Direction.LeftToRight)
                return cur.Prev;

            return cur.Next;
        }

        public bool Find(TNode example, out TNode result)
        {
            return Find(example, Direction.LeftToRight, out result);
        }

        public bool Find(TNode start, TNode example, out TNode result)
        {
            return Find(start, example, Direction.LeftToRight, out result);
        }

        public bool Find(TNode example, Direction dir, out TNode result)
        {
            return Find(GetFirst(dir), example, dir, out result);
        }

        public bool Find(TNode start, TNode example, Direction dir, out TNode result)
        {
            for (TNode n = start; n != GetEnd(dir); n = n.GetNext(dir))
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

        /// <summary>
        /// Inserts <c>newNode</c> to the left or right of <c>node</c>.
        /// </summary>
        /// <param name="node">The current node.</param>
        /// <param name="newNode">The new node.</param>
        /// <param name="dir">The direction to insert the new node.</param>
        /// <exception cref="System.ArgumentException">Thrown when the specified node is not owned by this linked list.</exception>
        public virtual void AddAfter(TNode node, TNode newNode, Direction dir)
        {
            if (_size == 0 && node == null)
                node = GetBegin(dir);

            if (node.List != this)
                throw new ArgumentException("node is not a member of this collection.", "node");

            newNode.Remove();
            newNode.Init(this);

            if (dir == Direction.RightToLeft)
                node = node.Prev;

            newNode.Next = node.Next;
            node.Next = newNode;
            newNode.Prev = node;
            newNode.Next.Prev = newNode;

            _size++;
        }

        public void AddAfter(TNode node, TNode newNode)
        {
            AddAfter(node, newNode, Direction.LeftToRight);
        }

        /// <summary>
        /// Adds all of the nodes from the enumerable collection.
        /// </summary>
        /// <param name="e">The enumerable collection.</param>
        public void AddRange(IEnumerable<TNode> e)
        {
            foreach (TNode node in e)
                Add(node);
        }

        public void AddRangeAfter(TNode node, IEnumerable<TNode> newNodes, Direction dir)
        {
            if (_size == 0 && node == null)
                node = GetBegin(dir);

            if (node.List != this)
                throw new ArgumentException("node is not a member of this collection.", "node");

            foreach (TNode newNode in newNodes)
            {
                AddAfter(node, newNode, dir);
                node = newNode;
            }
        }

        public void AddRangeAfter(TNode node, IEnumerable<TNode> newNodes)
        {
            AddRangeAfter(node, newNodes, Direction.LeftToRight);
        }
    }
}
