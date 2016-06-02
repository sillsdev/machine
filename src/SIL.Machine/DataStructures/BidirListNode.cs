namespace SIL.Machine.DataStructures
{
	public abstract class BidirListNode<TNode> : IBidirListNode<TNode> where TNode : BidirListNode<TNode>
	{
		private BidirList<TNode> _list;
		private TNode[] _next;
		private TNode[] _prev;

		public IBidirList<TNode> List { get { return _list; } }

		public TNode Next
		{
			get
			{
				if (_next == null)
					return null;

				return _next[0];
			}
		}

		public TNode Prev
		{
			get
			{
				if (_prev == null)
					return null;

				return _prev[0];
			}
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

			return List.GetNext((TNode) this, dir);
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

			return List.GetPrev((TNode) this, dir);
		}

		public bool Remove()
		{
			if (List == null)
				return false;

			return List.Remove((TNode) this);
		}

		protected internal virtual void Init(BidirList<TNode> list, int levels)
		{
			_list = list;
			_next = new TNode[levels];
			_prev = new TNode[levels];
			Levels = levels;
		}

		protected internal virtual void Clear()
		{
			_list = null;
			_next = null;
			_prev = null;
			Levels = 0;
		}

		internal int Levels { get; set; }

		internal TNode GetNext(int level)
		{
			return _next[level];
		}

		internal void SetNext(int level, TNode node)
		{
			_next[level] = node;
		}

		internal TNode GetPrev(int level)
		{
			return _prev[level];
		}
		
		internal void SetPrev(int level, TNode node)
		{
			_prev[level] = node;
		}
	}
}
