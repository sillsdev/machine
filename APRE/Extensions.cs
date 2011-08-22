using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.APRE
{
	public static class Extensions
	{
		#region IBidirList

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
			return new BidirListView<TNode>(first, last);
		}

		public static BidirListView<TNode> GetView<TNode>(this IBidirList<TNode> list, TNode first, Direction dir) where TNode : class, IBidirListNode<TNode>
		{
			return GetView(list, first, list.GetLast(dir), dir);
		}

		public static BidirListView<TNode> GetView<TNode>(this IBidirList<TNode> list, TNode first, TNode last, Direction dir) where TNode : class, IBidirListNode<TNode>
		{
			return new BidirListView<TNode>(dir == Direction.LeftToRight ? first : last, dir == Direction.LeftToRight ? last : first);
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

		#endregion

		#region IBidirTree

		public static void PreorderTraverse<TNode>(this IBidirTree<TNode> tree, Action<TNode> action) where TNode : class, IBidirTreeNode<TNode>
		{
			PreorderTraverse(tree, action, Direction.LeftToRight);
		}

		public static void PreorderTraverse<TNode>(this IBidirTree<TNode> tree, Action<TNode> action, Direction dir) where TNode : class, IBidirTreeNode<TNode>
		{
			TraverseNode(tree.Root, action, dir, true);
		}

		public static void PostorderTraverse<TNode>(this IBidirTree<TNode> tree, Action<TNode> action) where TNode : class, IBidirTreeNode<TNode>
		{
			PostorderTraverse(tree, action, Direction.LeftToRight);
		}

		public static void PostorderTraverse<TNode>(this IBidirTree<TNode> tree, Action<TNode> action, Direction dir) where TNode : class, IBidirTreeNode<TNode>
		{
			TraverseNode(tree.Root, action, dir, false);
		}

		private static void TraverseNode<TNode>(TNode node, Action<TNode> action, Direction dir, bool preorder) where TNode : class, IBidirTreeNode<TNode>
		{
			if (preorder)
				action(node);
			foreach (TNode child in node.Children.GetNodes(dir))
				TraverseNode(child, action, dir, preorder);
			if (!preorder)
				action(node);
		}

		public static IEnumerable<TNode> GetNodes<TNode>(this IBidirTree<TNode> tree) where TNode : class, IBidirTreeNode<TNode>
		{
			return GetNodes(tree, Direction.LeftToRight);
		}

		public static IEnumerable<TNode> GetNodes<TNode>(this IBidirTree<TNode> tree, Direction dir) where TNode : class, IBidirTreeNode<TNode>
		{
			var stack = new Stack<TNode>();
			stack.Push(tree.Root);
			while (stack.Any())
			{
				TNode node = stack.Pop();
				yield return node;
				foreach (TNode child in node.Children.GetNodes(dir))
					stack.Push(child);
			}
		}

		#endregion

		#region IBidirList<Annotation>

		public static BidirListView<Annotation<TOffset>> GetView<TOffset>(this IBidirList<Annotation<TOffset>> list, Span<TOffset> span)
		{
			Annotation<TOffset> startAnn;
			list.Find(new Annotation<TOffset>(span.StartSpan), Direction.LeftToRight, out startAnn);
			startAnn = startAnn == null ? list.First : startAnn.Next;

			Annotation<TOffset> endAnn;
			list.Find(new Annotation<TOffset>(span.EndSpan), Direction.LeftToRight, out endAnn);
			endAnn = endAnn == null ? list.First : (span.Contains(endAnn.Next.Span) ? endAnn.Next : endAnn);

			return list.GetView(startAnn, endAnn);
		}

		#endregion

		#region IEnumerable

		public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T item)
		{
			foreach (T i in source)
				yield return i;
			yield return item;
		}

		#endregion

		#region Generic

		public static bool IsOneOf<T>(this T item, params T[] list)
		{
			return list.Contains(item);
		}

		public static IEnumerable<T> ToEnumerable<T>(this T item)
		{
			yield return item;
		}

		#endregion
	}
}
