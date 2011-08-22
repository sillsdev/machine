namespace SIL.APRE
{
	public abstract class SkipListNode<TNode> : IBidirListNode<TNode> where TNode : SkipListNode<TNode>
	{
		private SkipList<TNode> _list;
		private State _leftToRightState;
		private State _rightToLeftState;

		private State GetState(Direction dir)
		{
			return dir == Direction.LeftToRight ? _leftToRightState : _rightToLeftState;
		}

		public IBidirList<TNode> List { get { return _list; } }

		public TNode Next
		{
			get { return GetNext(Direction.LeftToRight); }
		}

		public TNode Prev
		{
			get { return GetPrev(Direction.LeftToRight); }
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

			return GetState(dir).Next[0];
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

			return GetState(dir).Prev[0];
		}

		public bool Remove()
		{
			if (List == null)
				return false;

			return List.Remove((TNode) this);
		}

		internal void Init(SkipList<TNode> list, bool singleState)
		{
			_list = list;
			_leftToRightState = new State();
			_rightToLeftState = singleState ? _leftToRightState : new State();
		}

		internal void Clear()
		{
			_list = null;
			_leftToRightState = null;
			_rightToLeftState = null;
		}

		internal int GetLevels(Direction dir)
		{
			State state = GetState(dir);
			if (state == null || state.Next == null)
				return 0;
			return state.Next.Length;
		}

		internal void SetLevels(Direction dir, int levels)
		{
			State state = GetState(dir);

			state.Next = null;
			state.Prev = null;
			if (levels > 0)
			{
				state.Next = new TNode[levels];
				state.Prev = new TNode[levels];
			}
		}

		internal TNode GetNext(Direction dir, int level)
		{
			return GetState(dir).Next[level];
		}

		internal void SetNext(Direction dir, int level, TNode node)
		{
			GetState(dir).Next[level] = node;
		}

		internal TNode GetPrev(Direction dir, int level)
		{
			return GetState(dir).Prev[level];
		}
        
        internal void SetPrev(Direction dir, int level, TNode node)
        {
        	GetState(dir).Prev[level] = node;
        }

		class State
		{
			public TNode[] Next { get; set; }

			public TNode[] Prev { get; set; }
		}
	}
}
