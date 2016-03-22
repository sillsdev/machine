using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.DataStructures
{
	public abstract class BidirList<TNode> : IBidirList<TNode> where TNode : BidirListNode<TNode>
	{
		private readonly TNode _begin;
		private readonly TNode _end;
		private readonly IComparer<TNode> _comparer;

		private readonly Random _rand = new Random();
		private int _size;

		protected BidirList(IComparer<TNode> comparer, Func<bool, TNode> marginSelector)
		{
			_begin = marginSelector(true);
			_end = marginSelector(false);
			_begin.Init(this, 33);
			_begin.Levels = 1;
			_end.Init(this, 33);
			_end.Levels = 1;
			for (int i = 0; i < 33; i++)
			{
				_begin.SetNext(i, _end);
				_end.SetPrev(i, _begin);
			}
			_comparer = comparer;
		}

		public IComparer<TNode> Comparer
		{
			get { return _comparer; }
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

			// Determine the level of the new node. Generate a random number R. The number of
			// 1-bits before we encounter the first 0-bit is the level of the node. Since R is
			// 32-bit, the level can be at most 32.
			int level = 0;
			for (int r = _rand.Next(); (r & 1) == 1; r >>= 1)
			{
				level++;
				if (level == _begin.Levels)
				{
					_begin.Levels++;
					_end.Levels++;
					break;
				}
			}

			node.Remove();
			node.Init(this, level + 1);

			TNode cur = _begin;
			for (int i = _begin.Levels - 1; i >= 0; i--)
			{
				TNode next = cur.GetNext(i);
				while (next != _end)
				{
					if (_comparer.Compare(next, node) > 0)
						break;
					cur = next;
					next = cur.GetNext(i);
				}

				if (i <= level)
				{
					node.SetNext(i, cur.GetNext(i));
					cur.SetNext(i, node);
					node.SetPrev(i, cur);
					node.GetNext(i).SetPrev(i, node);
				}
			}
			_size++;
		}

		public virtual void Clear()
		{
			foreach (TNode node in this.ToArray())
				node.Clear();
			for (int i = 0; i < 33; i++)
			{
				_begin.SetNext(i, _end);
				_end.SetPrev(i, _begin);
			}
			_begin.Levels = 1;
			_end.Levels = 1;
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

		IEnumerator<TNode> IEnumerable<TNode>.GetEnumerator()
		{
			if (Count == 0)
				yield break;
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

			for (int i = 0; i < node.Levels; i++)
			{
				node.GetPrev(i).SetNext(i, node.GetNext(i));
				node.GetNext(i).SetPrev(i, node.GetPrev(i));
			}

			node.Clear();
			_size--;
			return true;
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

		public TNode First
		{
			get { return _size == 0 ? null : _begin.GetNext(0); }
		}

		public TNode Last
		{
			get { return _size == 0 ? null : _end.GetPrev(0); }
		}

		public TNode GetFirst(Direction dir)
		{
			if (dir == Direction.LeftToRight)
				return First;

			return Last;
		}

		public TNode GetLast(Direction dir)
		{
			if (dir == Direction.LeftToRight)
				return Last;

			return First;
		}

		public TNode GetNext(TNode cur)
		{
			return GetNext(cur, Direction.LeftToRight);
		}

		public TNode GetNext(TNode cur, Direction dir)
		{
			if (cur.List != this)
				throw new ArgumentException("cur is not a member of this collection.", "cur");

			if (dir == Direction.LeftToRight)
				return cur.Next;

			return cur.Prev;
		}

		public TNode GetPrev(TNode cur)
		{
			return GetPrev(cur, Direction.LeftToRight);
		}

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
			return Find(dir == Direction.LeftToRight ? _begin.GetNext(_begin.Levels - 1) : _end.GetPrev(_end.Levels - 1), example, dir, out result);
		}

		public bool Find(TNode start, TNode example, Direction dir, out TNode result)
		{
			TNode cur = dir == Direction.LeftToRight ? start.GetPrev(start.Levels - 1) : start.GetNext(start.Levels - 1);
			for (int i = start.Levels - 1; i >= 0; i--)
			{
				TNode next = dir == Direction.LeftToRight ? cur.GetNext(i) : cur.GetPrev(i);
				while (next != _end)
				{
					int res = _comparer.Compare(next, example);
					if (dir == Direction.RightToLeft)
						res = -res;
					if (res > 0)
						break;
					if (res == 0)
					{
						result = next;
						return true;
					}
					cur = next;
					next = dir == Direction.LeftToRight ? cur.GetNext(i) : cur.GetPrev(i);
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
	}
}
