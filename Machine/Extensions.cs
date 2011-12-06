using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine
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

		public static IEnumerable<TNode> GetNodes<TNode>(this IBidirList<TNode> list, Direction dir) where TNode : class, IBidirListNode<TNode>
		{
			if (list.Count == 0)
				return Enumerable.Empty<TNode>();
			return list.GetFirst(dir).GetNodes(dir);
		}

		public static IEnumerable<TNode> GetNodes<TNode>(this IBidirList<TNode> list, TNode first, TNode last) where TNode : class, IBidirListNode<TNode>
		{
			return first.GetNodes(last);
		}

		public static IEnumerable<TNode> GetNodes<TNode>(this IBidirList<TNode> list, TNode first, TNode last, Direction dir) where TNode : class, IBidirListNode<TNode>
		{
			return first.GetNodes(last, dir);
		}

		#endregion

		#region IBidirListNode

		public static TNode GetNext<TNode>(this IBidirListNode<TNode> cur, Direction dir, Func<TNode, TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			var node = (TNode) cur;
			do
			{
				node = node.GetNext(dir);
			}
			while (node != null && !filter((TNode)cur, node));
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

		public static IEnumerable<TNode> GetNodes<TNode>(this IBidirListNode<TNode> first) where TNode : class, IBidirListNode<TNode>
		{
			return GetNodes(first, Direction.LeftToRight);
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

		#region IBidirTreeNode

		public static void PreorderTraverse<TNode>(this IBidirTreeNode<TNode> root, Action<TNode> action) where TNode : class, IBidirTreeNode<TNode>
		{
			PreorderTraverse(root, action, Direction.LeftToRight);
		}

		public static void PreorderTraverse<TNode>(this IBidirTreeNode<TNode> root, Action<TNode> action, Direction dir) where TNode : class, IBidirTreeNode<TNode>
		{
			TraverseNode((TNode) root, action, dir, true);
		}

		public static void PostorderTraverse<TNode>(this IBidirTreeNode<TNode> root, Action<TNode> action) where TNode : class, IBidirTreeNode<TNode>
		{
			PostorderTraverse(root, action, Direction.LeftToRight);
		}

		public static void PostorderTraverse<TNode>(this IBidirTreeNode<TNode> root, Action<TNode> action, Direction dir) where TNode : class, IBidirTreeNode<TNode>
		{
			TraverseNode((TNode) root, action, dir, false);
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

		public static IEnumerable<TNode> GetNodes<TNode>(this IBidirTreeNode<TNode> root) where TNode : class, IBidirTreeNode<TNode>
		{
			return GetNodes(root, Direction.LeftToRight);
		}

		public static IEnumerable<TNode> GetNodes<TNode>(this IBidirTreeNode<TNode> root, Direction dir) where TNode : class, IBidirTreeNode<TNode>
		{
			var stack = new Stack<TNode>();
			stack.Push((TNode) root);
			while (stack.Any())
			{
				TNode node = stack.Pop();
				yield return node;
				foreach (TNode child in node.Children.GetNodes(dir))
					stack.Push(child);
			}
		}

		public static TNode GetRoot<TNode>(this IBidirTreeNode<TNode> node) where TNode : class, IBidirTreeNode<TNode>
		{
			var curNode = (TNode) node;
			while (curNode.Parent != null)
				curNode = curNode.Parent;
			return curNode;
		}
			
		#endregion

		#region IEnumerable

		public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T item)
		{
			foreach (T i in source)
				yield return i;
			yield return item;
		}

		public static IEnumerable<T> Clone<T>(this IEnumerable<T> source) where T : ICloneable<T>
		{
			foreach (T i in source)
				yield return i.Clone();
		}

		public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second,
			Func<TFirst, TSecond, TResult> resultSelector)
		{
			using (IEnumerator<TFirst> iterator1 = first.GetEnumerator())
			using (IEnumerator<TSecond> iterator2 = second.GetEnumerator())
			{
				while (iterator1.MoveNext() && iterator2.MoveNext())
					yield return resultSelector(iterator1.Current, iterator2.Current);
			} 
		}

		public static IEnumerable<Tuple<TFirst, TSecond>> Zip<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
		{
			using (IEnumerator<TFirst> iterator1 = first.GetEnumerator())
			using (IEnumerator<TSecond> iterator2 = second.GetEnumerator())
			{
				while (iterator1.MoveNext() && iterator2.MoveNext())
					yield return Tuple.Create(iterator1.Current, iterator2.Current);
			} 
		}

		public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector)
		{
			return source.MinBy(selector, Comparer<TKey>.Default);
		}

		public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer)
		{
			using (IEnumerator<TSource> sourceIterator = source.GetEnumerator())
			{
				if (!sourceIterator.MoveNext())
				{
					throw new InvalidOperationException("Sequence was empty");
				}
				TSource min = sourceIterator.Current;
				TKey minKey = selector(min);
				while (sourceIterator.MoveNext())
				{
					TSource candidate = sourceIterator.Current;
					TKey candidateProjected = selector(candidate);
					if (comparer.Compare(candidateProjected, minKey) < 0)
					{
						min = candidate;
						minKey = candidateProjected;
					}
				}
				return min;
			}
		}

		public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector)
		{
			return source.MaxBy(selector, Comparer<TKey>.Default);
		}

		public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer)
		{
			using (IEnumerator<TSource> sourceIterator = source.GetEnumerator())
			{
				if (!sourceIterator.MoveNext())
				{
					throw new InvalidOperationException("Sequence was empty");
				}
				TSource max = sourceIterator.Current;
				TKey maxKey = selector(max);
				while (sourceIterator.MoveNext())
				{
					TSource candidate = sourceIterator.Current;
					TKey candidateProjected = selector(candidate);
					if (comparer.Compare(candidateProjected, maxKey) > 0)
					{
						max = candidate;
						maxKey = candidateProjected;
					}
				}
				return max;
			}
		}

		public static IEnumerable<string> GetIDs<T>(this IEnumerable<T> source) where T : IIDBearer
		{
			return source.Select(idBearer => idBearer.ID);
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
