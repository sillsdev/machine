using System;
using System.Collections.Generic;

namespace SIL.APRE
{
	public static class BidirListExtensions
	{
		public static TNode GetFirst<TNode>(this IBidirList<TNode> list, Direction dir, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			TNode node = list.GetFirst(dir);
			while (node != null && !filter(node))
				node = node.GetNext(dir);
			return node;
		}

		public static TNode GetLast<TNode>(this IBidirList<TNode> list, Direction dir, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			TNode node = list.GetLast(dir);
			while (node != null && !filter(node))
				node = node.GetPrev(dir);
			return node;
		}

		public static TNode GetNext<TNode>(this IBidirList<TNode> list, TNode cur, Direction dir, Func<TNode, TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			return cur.GetNext(dir, filter);
		}

		public static TNode GetNext<TNode>(this IBidirList<TNode> list, TNode cur, Direction dir, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			return cur.GetNext(dir, filter);
		}

		public static TNode GetPrev<TNode>(this IBidirList<TNode> list, TNode cur, Direction dir, Func<TNode, TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			return cur.GetPrev(dir, filter);
		}

		public static TNode GetPrev<TNode>(this IBidirList<TNode> list, TNode cur, Direction dir, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			return cur.GetPrev(dir, filter);
		}

		public static TNode GetNext<TNode>(this IBidirListNode<TNode> cur, Direction dir, Func<TNode, TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			var node = (TNode) cur;
			do
			{
				node = node.GetNext(dir);
			}
			while (node != null && !filter((TNode) cur, node));
			return node;
		}

		public static TNode GetNext<TNode>(this IBidirListNode<TNode> cur, Direction dir, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			var node = (TNode) cur;
			do
			{
				node = node.GetNext(dir);
			}
			while (node != null && !filter(node));
			return node;
		}

		public static TNode GetPrev<TNode>(this IBidirListNode<TNode> cur, Direction dir, Func<TNode, TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			var node = (TNode) cur;
			do
			{
				node = node.GetPrev(dir);
			}
			while (node != null && !filter((TNode) cur, node));
			return node;
		}

		public static TNode GetPrev<TNode>(this IBidirListNode<TNode> cur, Direction dir, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			var node = (TNode) cur;
			do
			{
				node = node.GetPrev(dir);
			}
			while (node != null && !filter(node));
			return node;
		}

		public static BidirListView<TNode> GetView<TNode>(this IBidirList<TNode> list, TNode first) where TNode : class, IBidirListNode<TNode>
		{
			return GetView(list, first, list.Last);
		}

		public static BidirListView<TNode> GetView<TNode>(this IBidirList<TNode> list, TNode first, TNode last) where TNode : class, IBidirListNode<TNode>
		{
			return new BidirListView<TNode>(list, first, last);
		}

		public static BidirListView<TNode> GetView<TNode>(this IBidirList<TNode> list, TNode first, Direction dir) where TNode : class, IBidirListNode<TNode>
		{
			return GetView(list, first, list.GetLast(dir), dir);
		}

		public static BidirListView<TNode> GetView<TNode>(this IBidirList<TNode> list, TNode first, TNode last, Direction dir) where TNode : class, IBidirListNode<TNode>
		{
			return new BidirListView<TNode>(list, dir == Direction.LeftToRight ? first : last, dir == Direction.LeftToRight ? last : first);
		}

		public static IEnumerable<TNode> GetNodes<TNode>(this IBidirList<TNode> list, Direction dir) where TNode : class, IBidirListNode<TNode>
		{
			return list.GetFirst(dir).GetNodes(dir);
		}

		public static IEnumerable<TNode> GetNodes<TNode>(this IBidirList<TNode> list, TNode first, TNode last, Direction dir) where TNode : class, IBidirListNode<TNode>
		{
			return first.GetNodes(last, dir);
		}

		public static IEnumerable<TNode> GetNodes<TNode>(this IBidirListNode<TNode> first, TNode last) where TNode : class, IBidirListNode<TNode>
		{
			return GetNodes(first, last, Direction.LeftToRight);
		}

		public static IEnumerable<TNode> GetNodes<TNode>(this IBidirListNode<TNode> first, Direction dir) where TNode : class, IBidirListNode<TNode>
		{
			return GetNodes(first, first.List.GetLast(dir), dir);
		}

		public static IEnumerable<TNode> GetNodes<TNode>(this IBidirListNode<TNode> first, TNode last, Direction dir) where TNode : class, IBidirListNode<TNode>
		{
			for (var node = (TNode) first; node != last.GetNext(dir); node = node.GetNext(dir))
				yield return node;
		}
	}
}
