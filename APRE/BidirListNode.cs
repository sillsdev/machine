namespace SIL.APRE
{
	/// <summary>
	/// This is an abstract class that all bi-directional linked list nodes must extend. Having to specify the type
	/// of the class that extends this class is a little weird, but it allows us to have strongly-typed
	/// methods in the node class that can manipulate the owning linked list.
	/// </summary>
	/// <typeparam name="TNode">Item Type, must be the type of the class that extends this class.</typeparam>
	public abstract class BidirListNode<TNode> : IBidirListNode<TNode> where TNode : BidirListNode<TNode>
	{
		private BidirList<TNode> _list;

		/// <summary>
		/// Gets the linked list that owns this record.
		/// </summary>
		/// <value>The owning linked list.</value>
		public IBidirList<TNode> List { get { return _list; } }

		/// <summary>
		/// Gets the next node in the owning linked list.
		/// </summary>
		/// <value>The next node.</value>
		public TNode Next { get; internal set; }

		/// <summary>
		/// Gets the previous node in the owning linked list.
		/// </summary>
		/// <value>The previous node.</value>
		public TNode Prev { get; internal set; }

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

		/// <summary>
		/// Removes this node from the owning linked list.
		/// </summary>
		/// <returns><c>true</c> if the node is a member of a linked list, otherwise <c>false</c></returns>
		public bool Remove()
		{
			if (List == null)
				return false;

			return List.Remove((TNode)this);
		}

		/// <summary>
		/// Inserts the specified node to the right or left of this node.
		/// </summary>
		/// <param name="newNode">The new node.</param>
		/// <param name="dir">The direction to insert the node.</param>
		public void Insert(TNode newNode, Direction dir)
		{
			if (List == null)
				return;

			_list.Insert(newNode, (TNode)this, dir);
		}

		protected internal virtual void Init(BidirList<TNode> list)
		{
			_list = list;
		}

		protected internal virtual void Clear()
		{
			_list = null;
			Next = null;
			Prev = null;
		}
	}
}
