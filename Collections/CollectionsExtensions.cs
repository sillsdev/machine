using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Collections
{
	public static class CollectionsExtensions
	{
		#region IBidirList

		public static TNode GetFirst<TNode>(this IBidirList<TNode> list, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			return GetFirst(list, Direction.LeftToRight, filter);
		}

		public static TNode GetFirst<TNode>(this IBidirList<TNode> list, Direction dir, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			TNode node = list.GetFirst(dir);
			while (node != null && node != list.GetEnd(dir) && !filter(node))
				node = node.GetNext(dir);
			return node;
		}

		public static TNode GetLast<TNode>(this IBidirList<TNode> list, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			return GetLast(list, Direction.LeftToRight, filter);
		}

		public static TNode GetLast<TNode>(this IBidirList<TNode> list, Direction dir, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			TNode node = list.GetLast(dir);
			while (node != null && node != list.GetBegin(dir) && !filter(node))
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

		public static TNode GetNext<TNode>(this IBidirListNode<TNode> node, Func<TNode, TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			return GetNext(node, Direction.LeftToRight, filter);
		}

		public static TNode GetNext<TNode>(this IBidirListNode<TNode> node, Direction dir, Func<TNode, TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			var cur = (TNode) node;
			do
			{
				cur = cur.GetNext(dir);
			}
			while (cur != node.List.GetEnd(dir) && !filter((TNode) node, cur));
			return cur;
		}

		public static TNode GetNext<TNode>(this IBidirListNode<TNode> node, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			return GetNext(node, Direction.LeftToRight, filter);
		}

		public static TNode GetNext<TNode>(this IBidirListNode<TNode> node, Direction dir, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			var cur = (TNode) node;
			do
			{
				cur = cur.GetNext(dir);
			}
			while (cur != node.List.GetEnd(dir) && !filter(cur));
			return cur;
		}

		public static TNode GetPrev<TNode>(this IBidirListNode<TNode> node, Func<TNode, TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			return GetPrev(node, Direction.LeftToRight, filter);
		}

		public static TNode GetPrev<TNode>(this IBidirListNode<TNode> node, Direction dir, Func<TNode, TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			var cur = (TNode) node;
			do
			{
				cur = cur.GetPrev(dir);
			}
			while (cur != node.List.GetBegin(dir) && !filter((TNode) node, cur));
			return cur;
		}

		public static TNode GetPrev<TNode>(this IBidirListNode<TNode> node, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			return GetPrev(node, Direction.LeftToRight, filter);
		}

		public static TNode GetPrev<TNode>(this IBidirListNode<TNode> node, Direction dir, Func<TNode, bool> filter) where TNode : class, IBidirListNode<TNode>
		{
			var cur = (TNode) node;
			do
			{
				cur = cur.GetPrev(dir);
			}
			while (cur != node.List.GetBegin(dir) && !filter(cur));
			return cur;
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
			DepthFirstTraverseNode(root, action, dir, true);
		}

		public static void PostorderTraverse<TNode>(this IBidirTreeNode<TNode> root, Action<TNode> action) where TNode : class, IBidirTreeNode<TNode>
		{
			PostorderTraverse(root, action, Direction.LeftToRight);
		}

		public static void PostorderTraverse<TNode>(this IBidirTreeNode<TNode> root, Action<TNode> action, Direction dir) where TNode : class, IBidirTreeNode<TNode>
		{
			DepthFirstTraverseNode(root, action, dir, false);
		}

		private static void DepthFirstTraverseNode<TNode>(IBidirTreeNode<TNode> node, Action<TNode> action, Direction dir, bool preorder) where TNode : class, IBidirTreeNode<TNode>
		{
			if (preorder)
				action((TNode) node);
			if (!node.IsLeaf)
			{
				foreach (TNode child in node.Children.GetNodes(dir))
					DepthFirstTraverseNode(child, action, dir, preorder);
			}
			if (!preorder)
				action((TNode) node);
		}

		public static void LevelOrderTraverse<TNode>(this IBidirTreeNode<TNode> root, Action<TNode> action) where TNode : class, IBidirTreeNode<TNode>
		{
			LevelOrderTraverse(root, action, Direction.LeftToRight);
		}

		public static void LevelOrderTraverse<TNode>(this IBidirTreeNode<TNode> root, Action<TNode> action, Direction dir) where TNode : class, IBidirTreeNode<TNode>
		{
			var queue = new Queue<TNode>();
			queue.Enqueue((TNode)root);
			while (queue.Count > 0)
			{
				TNode node = queue.Dequeue();
				action(node);
				if (!node.IsLeaf)
				{
					foreach (TNode child in node.Children.GetNodes(dir))
						queue.Enqueue(child);
				}
			}
		}

		public static IEnumerable<TNode> GetNodesDepthFirst<TNode>(this IBidirTreeNode<TNode> root) where TNode : class, IBidirTreeNode<TNode>
		{
			return GetNodesDepthFirst(root, Direction.LeftToRight);
		}

		public static IEnumerable<TNode> GetNodesDepthFirst<TNode>(this IBidirTreeNode<TNode> root, Direction dir) where TNode : class, IBidirTreeNode<TNode>
		{
			yield return (TNode) root;

			if (!root.IsLeaf)
			{
				foreach (TNode child in root.Children.GetNodes(dir))
				{
					foreach (TNode node in child.GetNodesDepthFirst(dir))
						yield return node;
				}
			}
		}

		public static IEnumerable<TNode> GetNodesBreadthFirst<TNode>(this IBidirTreeNode<TNode> root) where TNode : class, IBidirTreeNode<TNode>
		{
			return GetNodesBreadthFirst(root, Direction.LeftToRight);
		}

		public static IEnumerable<TNode> GetNodesBreadthFirst<TNode>(this IBidirTreeNode<TNode> root, Direction dir) where TNode : class, IBidirTreeNode<TNode>
		{
			var queue = new Queue<TNode>();
			queue.Enqueue((TNode) root);
			while (queue.Count > 0)
			{
				TNode node = queue.Dequeue();
				yield return node;
				if (!node.IsLeaf)
				{
					foreach (TNode child in node.Children.GetNodes(dir))
						queue.Enqueue(child);
				}
			}
		}

		public static int DescendantCount<TNode>(this IBidirTreeNode<TNode> node) where TNode : class, IBidirTreeNode<TNode>
		{
			return node.Children.Count + node.Children.Sum(child => child.DescendantCount());
		}

		public static TNode GetNextDepthFirst<TNode>(this IBidirTreeNode<TNode> node) where TNode : class, IBidirTreeNode<TNode>
		{
			return node.GetNextDepthFirst(Direction.LeftToRight);
		}

		public static TNode GetNextDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Direction dir) where TNode : class, IBidirTreeNode<TNode>
		{
			if (!node.IsLeaf)
				return node.Children.GetFirst(dir);

			IBidirTreeNode<TNode> parent = node;
			do
			{
				node = parent;
				TNode next = node.GetNext(dir);
				if (next != node.List.GetEnd(dir))
					return next;
				parent = node.Parent;
			}
			while (parent != null);

			return node.GetNext(dir);
		}

		public static TNode GetNextDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Func<TNode, bool> filter) where TNode : class, IBidirTreeNode<TNode>
		{
			return node.GetNextDepthFirst(Direction.LeftToRight, filter);
		}

		public static TNode GetNextDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Direction dir, Func<TNode, bool> filter) where TNode : class, IBidirTreeNode<TNode>
		{
			var cur = (TNode) node;
			do
			{
				cur = cur.GetNextDepthFirst(dir);
			}
			while (cur != null && cur != cur.List.GetEnd(dir) && !filter(cur));
			return cur;
		}

		public static TNode GetNextDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Func<TNode, TNode, bool> filter) where TNode : class, IBidirTreeNode<TNode>
		{
			return node.GetNextDepthFirst(Direction.LeftToRight, filter);
		}

		public static TNode GetNextDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Direction dir, Func<TNode, TNode, bool> filter) where TNode : class, IBidirTreeNode<TNode>
		{
			var cur = (TNode) node;
			do
			{
				cur = cur.GetNextDepthFirst(dir);
			}
			while (cur != null && cur != cur.List.GetEnd(dir) && !filter((TNode) node, cur));
			return cur;
		}

		public static TNode GetPrevDepthFirst<TNode>(this IBidirTreeNode<TNode> node) where TNode : class, IBidirTreeNode<TNode>
		{
			return node.GetPrevDepthFirst(Direction.LeftToRight);
		}

		public static TNode GetPrevDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Direction dir) where TNode : class, IBidirTreeNode<TNode>
		{
			if (!node.IsLeaf)
				return node.Children.GetLast(dir);

			IBidirTreeNode<TNode> parent = node;
			do
			{
				node = parent;
				TNode prev = node.GetPrev(dir);
				if (prev != node.List.GetBegin(dir))
					return prev;
				parent = node.Parent;
			}
			while (parent != null);

			return node.GetPrev(dir);
		}

		public static TNode GetPrevDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Func<TNode, bool> filter) where TNode : class, IBidirTreeNode<TNode>
		{
			return node.GetPrevDepthFirst(Direction.LeftToRight, filter);
		}

		public static TNode GetPrevDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Direction dir, Func<TNode, bool> filter) where TNode : class, IBidirTreeNode<TNode>
		{
			var cur = (TNode) node;
			do
			{
				cur = cur.GetPrevDepthFirst(dir);
			}
			while (cur != null && cur != cur.List.GetBegin(dir) && !filter(cur));
			return cur;
		}

		public static TNode GetPrevDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Func<TNode, TNode, bool> filter) where TNode : class, IBidirTreeNode<TNode>
		{
			return node.GetPrevDepthFirst(Direction.LeftToRight, filter);
		}

		public static TNode GetPrevDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Direction dir, Func<TNode, TNode, bool> filter) where TNode : class, IBidirTreeNode<TNode>
		{
			var cur = (TNode) node;
			do
			{
				cur = cur.GetPrevDepthFirst(dir);
			}
			while (cur != null && cur != cur.List.GetBegin(dir) && !filter((TNode) node, cur));
			return cur;
		}

		#endregion

		#region IEnumerable

		public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T item)
		{
			foreach (T i in source)
				yield return i;
			yield return item;
		}

		public static IEnumerable<T> DeepClone<T>(this IEnumerable<T> source) where T : IDeepCloneable<T>
		{
			foreach (T i in source)
				yield return i.DeepClone();
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

		public static IEnumerable<Tuple<TFirst, TSecond, TThird>> Zip<TFirst, TSecond, TThird>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third)
		{
			using (IEnumerator<TFirst> iterator1 = first.GetEnumerator())
			using (IEnumerator<TSecond> iterator2 = second.GetEnumerator())
			using (IEnumerator<TThird> iterator3 = third.GetEnumerator())
			{
				while (iterator1.MoveNext() && iterator2.MoveNext() && iterator3.MoveNext())
					yield return Tuple.Create(iterator1.Current, iterator2.Current, iterator3.Current);
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

		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			return source.DistinctBy(keySelector, EqualityComparer<TKey>.Default);
		}

		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
			IEqualityComparer<TKey> comparer)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			if (keySelector == null)
			{
				throw new ArgumentNullException("keySelector");
			}
			if (comparer == null)
			{
				throw new ArgumentNullException("comparer");
			}

			var knownKeys = new HashSet<TKey>(comparer);
			foreach (TSource element in source)
			{
				if (knownKeys.Add(keySelector(element)))
					yield return element;
			}
		}

		public static int GetSequenceHashCode<T>(this IEnumerable<T> source)
		{
			return GetSequenceHashCode(source, EqualityComparer<T>.Default);
		}

		public static int GetSequenceHashCode<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
		{
			return source.Aggregate(23, (code, item) => code * 31 + (item == null ? 0 : comparer.GetHashCode(item)));
		}

		public static int SequenceCompare<T>(this IEnumerable<T> x, IEnumerable<T> y)
		{
			return SequenceCompare(x, y, Comparer<T>.Default);
		}

		public static int SequenceCompare<T>(this IEnumerable<T> x, IEnumerable<T> y, IComparer<T> comparer)
		{
			int result = 0;
			using (IEnumerator<T> iteratorX = x.GetEnumerator())
			using (IEnumerator<T> iteratorY = y.GetEnumerator())
			{
				bool hasValueX = iteratorX.MoveNext();
				bool hasValueY = iteratorY.MoveNext();
				while (hasValueX && hasValueY)
				{
					int compare = comparer.Compare(iteratorX.Current, iteratorY.Current);
					if (compare != 0)
					{
						result = compare;
						break;
					}

					hasValueX = iteratorX.MoveNext();
					hasValueY = iteratorY.MoveNext();
				}

				if (result == 0)
				{
					if (hasValueX && !hasValueY)
						result = 1;
					else if (!hasValueX && hasValueY)
						result = -1;
				}
			}

			return result;
		}

		public static IEnumerable<T> Items<T>(this IEnumerable<T> source, Direction dir)
		{
			return dir == Direction.LeftToRight ? source : source.Reverse();
		}

		public static int IndexOf<T>(this IEnumerable<T> source, T value)
		{
			return source.IndexOf(value, null);
		}

		public static int IndexOf<T>(this IEnumerable<T> source, T value, IEqualityComparer<T> comparer)
		{
			comparer = comparer ?? EqualityComparer<T>.Default;
			var found = source
				.Select((a, i) => new { a, i })
				.FirstOrDefault(x => comparer.Equals(x.a, value));
			return found == null ? -1 : found.i;
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

		#region IComparer

		public static IComparer<T> Reverse<T>(this IComparer<T> comparer)
		{
			if (comparer == null)
				throw new ArgumentNullException("comparer");
			return new ReverseComparer<T>(comparer);
		}

		#endregion

		#region IList

		public static int BinarySearch<T>(this IList<T> list, T item)
		{
			return BinarySearch(list, item, Comparer<T>.Default);
		}

		public static int BinarySearch<T>(this IList<T> list, T item, IComparer<T> comparer)
		{
			return BinarySearch(list, item, i => i, comparer);
		}

		public static int BinarySearch<TItem, TKey>(this IList<TItem> list, TItem item, Func<TItem, TKey> keySelector)
		{
			return BinarySearch(list, item, keySelector, Comparer<TKey>.Default);
		}

		public static int BinarySearch<TItem, TKey>(this IList<TItem> list, TItem item, Func<TItem, TKey> keySelector, IComparer<TKey> comparer)
		{
			if (list == null)
			{
				throw new ArgumentNullException("list");
			}
			if (comparer == null)
			{
				throw new ArgumentNullException("comparer");
			}

			int lower = 0;
			int upper = list.Count - 1;

			while (lower <= upper)
			{
				int middle = lower + (upper - lower) / 2;
				int comparisonResult = comparer.Compare(keySelector(item), keySelector(list[middle]));
				if (comparisonResult < 0)
				{
					upper = middle - 1;
				}
				else if (comparisonResult > 0)
				{
					lower = middle + 1;
				}
				else
				{
					return middle;
				}
			}

			return ~lower;
		}

		public static ReadOnlyList<T> ToReadOnlyList<T>(this IList<T> list)
		{
			return new ReadOnlyList<T>(list);
		}

		#endregion

		#region IDictionary

		public static void UpdateValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> defaultValueSelector, Func<TValue, TValue> valueSelector)
		{
			TValue value;
			if (!dictionary.TryGetValue(key, out value))
				value = defaultValueSelector();

			dictionary[key] = valueSelector(value);
		}

		public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> defaultValueSelector)
		{
			TValue value;
			if (!dictionary.TryGetValue(key, out value))
			{
				value = defaultValueSelector();
				dictionary[key] = value;
			}
			return value;
		}

		public static ReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
		{
			return new ReadOnlyDictionary<TKey, TValue>(dictionary);
		}

		#endregion

		#region ICollection

		public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
		{
			foreach (T item in items)
				collection.Add(item);
		}

		public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this ICollection<T> collection)
		{
			return new ReadOnlyCollection<T>(collection);
		}

		public static void RemoveAll<T>(this ICollection<T> collection, Func<T, bool> predicate)
		{
			foreach (T item in collection.Where(predicate).ToArray())
				collection.Remove(item);
		}

		#endregion

		#region ISet

		public static ReadOnlySet<T> ToReadOnlySet<T>(this ISet<T> set)
		{
			return new ReadOnlySet<T>(set);
		}

		#endregion

		#region IObservableList

		public static ReadOnlyObservableList<T> ToReadOnlyObservableList<T>(this IObservableList<T> list)
		{
			return new ReadOnlyObservableList<T>(list);
		}

		#endregion

		#region IObservableCollection

		public static ReadOnlyObservableCollection<T> ToReadOnlyObservableCollection<T>(this IObservableCollection<T> collection)
		{
			return new ReadOnlyObservableCollection<T>(collection);
		}

		#endregion

		#region IKeyedCollection

		public static ReadOnlyKeyedCollection<TKey, TItem> ToReadOnlyKeyedCollection<TKey, TItem>(this IKeyedCollection<TKey, TItem> collection)
		{
			return new ReadOnlyKeyedCollection<TKey, TItem>(collection);
		}

		#endregion

		#region IEqualityComparer

		public static IEqualityComparer<T> ToTypesafe<T>(this IEqualityComparer comparer)
		{
			return new WrapperEqualityComparer<T>(comparer);
		}

		#endregion
	}
}
