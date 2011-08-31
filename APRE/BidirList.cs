using System;
using System.Collections;
using System.Collections.Generic;

namespace SIL.APRE
{
    /// <summary>
    /// This is a bi-directional list. It is optimized for list traversal in either direction.
    /// </summary>
    /// <typeparam name="TNode">Item Type, must be the type of the class that the linked list handles.</typeparam>
    public class BidirList<TNode> : IBidirList<TNode> where TNode : BidirListNode<TNode>
    {
        private TNode _first;
    	private TNode _last;
        private int _size;

    	private readonly IEqualityComparer<TNode> _comparer; 

		public BidirList()
			: this(EqualityComparer<TNode>.Default)
		{
		}

		public BidirList(IEqualityComparer<TNode> comparer)
		{
			_comparer = comparer;
		}

    	public int Count
        {
            get
            {
                return _size;
            }
        }

        bool ICollection<TNode>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Adds the specified node to the end of this list.
        /// </summary>
        /// <param name="node">The node.</param>
        public virtual void Add(TNode node)
        {
			Insert(node, _last, Direction.LeftToRight);
        }

        public virtual void Clear()
        {
			foreach (TNode node in this)
				node.Clear();
        	_first = null;
        	_last = null;
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

			TNode prev = node.Prev;
			if (prev == null)
				_first = node.Next;
			else
				prev.Next = node.Next;

			TNode next = node.Next;
			if (next == null)
				_last = node.Prev;
			else
				next.Prev = node.Prev;

			node.Clear();

            _size--;

            return true;
        }

        IEnumerator<TNode> IEnumerable<TNode>.GetEnumerator()
        {
            for (TNode node = First; node != null; node = node.Next)
                yield return node;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TNode>) this).GetEnumerator();
        }

        /// <summary>
        /// Gets the first node in this list.
        /// </summary>
        /// <value>The first node.</value>
        public TNode First
        {
            get { return _first; } 
        }

        /// <summary>
        /// Gets the last node in this list.
        /// </summary>
        /// <value>The last node.</value>
        public TNode Last
        {
            get { return _last; }
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
			for (TNode n = start; n != null; n = n.GetNext(dir))
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

		public IBidirListView<TNode> GetView(TNode first)
		{
			return GetView(first, Direction.LeftToRight);
		}

		public IBidirListView<TNode> GetView(TNode first, TNode last)
		{
			return GetView(first, last, Direction.LeftToRight);
		}

		public IBidirListView<TNode> GetView(TNode first, Direction dir)
		{
			return GetView(first, GetLast(dir), dir);
		}

		public IBidirListView<TNode> GetView(TNode first, TNode last, Direction dir)
		{
			return new BidirListView<TNode>(dir == Direction.LeftToRight ? first : last, dir == Direction.LeftToRight ? last : first, _comparer);
		}

    	/// <summary>
        /// Inserts <c>newNode</c> to the left or right of <c>node</c>.
        /// </summary>
        /// <param name="newNode">The new node.</param>
        /// <param name="node">The current node.</param>
        /// <param name="dir">The direction to insert the new node.</param>
		/// <exception cref="System.ArgumentException">Thrown when the specified node is not owned by this linked list.</exception>
        public virtual void Insert(TNode newNode, TNode node, Direction dir)
        {
			if (newNode.List == this)
				throw new ArgumentException("newNode is already a member of this collection.", "newNode");
            if (node != null && node.List != this)
                throw new ArgumentException("node is not a member of this collection.", "node");

			newNode.Init(this);

            if (dir == Direction.LeftToRight)
            {
				newNode.Next = node == null ? _first : node.Next;
				if (node != null)
					node.Next = newNode;
				newNode.Prev = node;
				if (newNode.Next != null)
					newNode.Next.Prev = newNode;
            }
            else
            {
				newNode.Prev = node == null ? null : node.Prev;
				if (node != null)
					node.Prev = newNode;
				newNode.Next = node;
				if (newNode.Prev != null)
					newNode.Prev.Next = newNode;
            }

			if (newNode.Next == null)
				_last = newNode;

			if (newNode.Prev == null)
				_first = newNode;

            _size++;
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
    }
}
