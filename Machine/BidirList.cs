using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine
{
	public class BidirList<TNode> : IBidirList<TNode> where TNode : BidirListNode<TNode>
	{
		private readonly State _leftToRightState;
		private readonly State _rightToLeftState;
		private readonly Random _rand = new Random();
		private int _size;

		public BidirList()
			: this(Comparer<TNode>.Default)
		{
		}

		public BidirList(IComparer<TNode> comparer)
		{
			_leftToRightState = new State(comparer);
			_rightToLeftState = _leftToRightState;
		}

		public BidirList(IComparer<TNode> leftToRightComparer, IComparer<TNode> rightToLeftComparer)
		{
			_leftToRightState = new State(leftToRightComparer);
			_rightToLeftState = new State(rightToLeftComparer);
		}

		private State GetState(Direction dir)
		{
			return dir == Direction.LeftToRight ? _leftToRightState : _rightToLeftState;
		}

		public int GetLevels(Direction dir)
		{
			return GetState(dir).Levels;
		}

		public IComparer<TNode> GetComparer(Direction dir)
		{
			return GetState(dir).Comparer;
		}

		public int Count
		{
			get { return _size; }
		}

		bool ICollection<TNode>.IsReadOnly
		{
			get { return false; }
		}

		public virtual void Add(TNode node)
		{
			if (node.List == this)
				throw new ArgumentException("node is already a member of this collection.", "node");

			node.Init(this, _leftToRightState == _rightToLeftState);
			Add(node, Direction.LeftToRight);
			if (_leftToRightState != _rightToLeftState)
				Add(node, Direction.RightToLeft);
			_size++;
		}

		private void Add(TNode node, Direction dir)
		{
			State state = GetState(dir);
			// Determine the level of the new node. Generate a random number R. The number of
			// 1-bits before we encounter the first 0-bit is the level of the node. Since R is
			// 32-bit, the level can be at most 32.
			int level = 0;
			for (int r = _rand.Next(); (r & 1) == 1; r >>= 1)
			{
				level++;
				if (level == state.Levels)
				{
					state.Levels++;
					break;
				}
			}

			node.SetLevels(dir, level + 1);

			TNode cur = null;
			for (int i = state.Levels - 1; i >= 0; i--)
			{
				TNode next = cur == null ? state.First[i] : cur.GetNext(dir, i);
				while (next != null)
				{
					if (state.Comparer.Compare(next, node) > 0)
						break;
					cur = next;
					next = cur.GetNext(dir, i);
				}

				if (i <= level)
				{
					node.SetNext(dir, i, cur == null ? state.First[i] : cur.GetNext(dir, i));
					if (cur != null)
						cur.SetNext(dir, i, node);
					else
						state.First[i] = node;
					node.SetPrev(dir, i, cur);
					if (node.GetNext(dir, i) != null)
						node.GetNext(dir, i).SetPrev(dir, i, node);
					else
						state.Last[i] = node;
				}
			}
		}

		public virtual void Clear()
		{
			foreach (TNode node in this.ToArray())
				node.Clear();
			Clear(Direction.LeftToRight);
			if (_leftToRightState != _rightToLeftState)
				Clear(Direction.RightToLeft);
			_size = 0;
		}

		private void Clear(Direction dir)
		{
			State state = GetState(dir);
			for (int i = 0; i < state.First.Length; i++)
				state.First[i] = null;
			for (int i = 0; i < state.Last.Length; i++)
				state.Last[i] = null;
			state.Levels = 1;
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

		IEnumerator<TNode> IEnumerable<TNode>.GetEnumerator()
		{
			for (TNode node = First; node != End; node = node.Next)
				yield return node;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<TNode>) this).GetEnumerator();
		}

		public virtual bool Remove(TNode node)
		{
			if (node.List != this)
				return false;

			Remove(node, Direction.LeftToRight);
			if (_leftToRightState != _rightToLeftState)
				Remove(node, Direction.RightToLeft);

			node.Clear();
			_size--;
			return true;
		}

		private void Remove(TNode node, Direction dir)
		{
			State state = GetState(dir);
			for (int i = 0; i < node.GetLevels(dir); i++)
			{
				TNode prev = node.GetPrev(dir, i);
				if (prev == null)
					state.First[i] = node.GetNext(dir, i);
				else
					prev.SetNext(dir, i, node.GetNext(dir, i));

				TNode next = node.GetNext(dir, i);
				if (next == null)
					state.Last[i] = node.GetPrev(dir, i);
				else
					next.SetPrev(dir, i, node.GetPrev(dir, i));
			}
		}

		public TNode Begin
		{
			get { return null; }
		}

		public TNode End
		{
			get { return null; }
		}

		public TNode GetBegin(Direction dir)
		{
			return null;
		}

		public TNode GetEnd(Direction dir)
		{
			return null;
		}

		public TNode First
		{
			get { return GetFirst(Direction.LeftToRight); }
		}

		public TNode Last
		{
			get { return GetLast(Direction.LeftToRight); }
		}

		public TNode GetFirst(Direction dir)
		{
			return GetState(dir).First[0];
		}

		public TNode GetLast(Direction dir)
		{
			return GetState(dir).Last[0];
		}

		public TNode GetNext(TNode cur)
		{
			return GetNext(cur, Direction.LeftToRight);
		}

		public TNode GetNext(TNode cur, Direction dir)
		{
			if (cur.List != this)
				throw new ArgumentException("cur is not a member of this collection.", "cur");

			return cur.GetNext(dir);
		}

		public TNode GetPrev(TNode cur)
		{
			return GetPrev(cur, Direction.LeftToRight);
		}

		public TNode GetPrev(TNode cur, Direction dir)
		{
			if (cur.List != this)
				throw new ArgumentException("cur is not a member of this collection.", "cur");

			return cur.GetPrev(dir);
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
			State state = GetState(dir);
			return Find(state.First[state.Levels - 1], example, dir, out result);
		}

		public bool Find(TNode start, TNode example, Direction dir, out TNode result)
		{
			State state = GetState(dir);
			TNode cur = null;
			for (int i = start.GetLevels(dir) - 1; i >= 0; i--)
			{
				TNode next = cur == null ? (i == start.GetLevels(dir) - 1 ? start : state.First[i]) : cur.GetNext(dir, i);
				while (next != null)
				{
					int res = state.Comparer.Compare(next, example);
					if (res > 0)
						break;
					if (res == 0)
					{
						result = next;
						return true;
					}
					cur = next;
					next = cur.GetNext(dir, i);
				}
			}
			result = cur;
			return false;
		}

		public void AddRange(IEnumerable<TNode> nodes)
		{
			foreach (TNode ann in nodes)
				Add(ann);
		}

		class State
		{
			private readonly TNode[] _first;
			private readonly TNode[] _last;
			private readonly IComparer<TNode> _comparer;

			public State(IComparer<TNode> comparer)
			{
				_first = new TNode[33];
				_last = new TNode[33];
				_comparer = comparer;
				Levels = 1;
			}

			public TNode[] First
			{
				get { return _first; }
			}

			public TNode[] Last
			{
				get { return _last;
				}
			}

			public IComparer<TNode> Comparer
			{
				get { return _comparer; }
			}

			public int Levels { get; set; }
		}
	}
}
