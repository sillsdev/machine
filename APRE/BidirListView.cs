using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.APRE
{
	public class BidirListView<TNode> : IBidirList<TNode> where TNode : class, IBidirListNode<TNode>
	{
		private readonly IBidirList<TNode> _list;
		private TNode _first;
		private TNode _last;

		public BidirListView(TNode first, TNode last)
		{
			_list = first.List;
			_first = first;
			_last = last;
		}

		public bool IsValid
		{
			get { return _first.List == _list && _last.List == _list; }
		} 

		public void SlideNext(int num, Direction dir)
		{
			for (int i = 0; i < num; i++)
			{
				TNode next = _first.GetNext(dir);
				if (next != null)
					_first = next;
				next = _last.GetNext(dir);
				if (next != null)
					_last = next;
			}
		}

		public void SlidePrev(int num, Direction dir)
		{
			for (int i = 0; i < num; i++)
			{
				TNode prev = _first.GetPrev(dir);
				if (prev != null)
					_first = prev;
				prev = _last.GetPrev(dir);
				if (prev != null)
					_last = prev;
			}
		}

		public IEnumerator<TNode> GetEnumerator()
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			for (TNode node = _first; node != _last.Next; node = node.Next)
				yield return node;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(TNode item)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			_list.Add(item);
			if (item.Next == _first)
				_first = item;
			if (item.Prev == _last)
				_last = item;
		}

		public void Clear()
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			foreach (TNode node in this.ToArray())
				node.Remove();
			_first = null;
			_last = null;
		}

		public bool Contains(TNode item)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			return Enumerable.Contains(this, item);
		}

		public void CopyTo(TNode[] array, int arrayIndex)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			foreach (TNode node in this)
				array[arrayIndex++] = node;
		}

		public bool Remove(TNode item)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			TNode next = item.Next;
			TNode prev = item.Prev;
			if (_list.Remove(item))
			{
				if (_first == item)
					_first = next;
				if (_last == item)
					_last = prev;
				return true;
			}
			return false;
		}

		public int Count
		{
			get
			{
				if (!IsValid)
					throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

				return this.Count();
			}
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public TNode First
		{
			get
			{
				if (!IsValid)
					throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

				return _first;
			}
		}

		public TNode Last
		{
			get
			{
				if (!IsValid)
					throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

				return _last;
			}
		}

		public TNode GetFirst(Direction dir)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			return dir == Direction.LeftToRight ? _first : _last;
		}

		public TNode GetLast(Direction dir)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			return dir == Direction.LeftToRight ? _last : _first;
		}

		public TNode GetNext(TNode cur)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			return _list.GetNext(cur);
		}

		public TNode GetNext(TNode cur, Direction dir)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			return _list.GetNext(cur, dir);
		}

		public TNode GetPrev(TNode cur)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			return _list.GetPrev(cur);
		}

		public TNode GetPrev(TNode cur, Direction dir)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			return _list.GetPrev(cur, dir);
		}

		public bool Find(TNode node, Direction dir, out TNode result)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			return _list.Find(node, dir, out result);
		}

		public bool Find(TNode start, TNode node, Direction dir, out TNode result)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			return _list.Find(start, node, dir, out result);
		}
	}
}
