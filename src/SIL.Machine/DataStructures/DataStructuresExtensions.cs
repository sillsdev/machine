using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.DataStructures
{
    public static class DataStructuresExtensions
    {
        #region IBidirList

        public static TNode GetFirst<TNode>(this IBidirList<TNode> list, Func<TNode, bool> filter)
            where TNode : class, IBidirListNode<TNode>
        {
            return GetFirst(list, Direction.LeftToRight, filter);
        }

        public static TNode GetFirst<TNode>(this IBidirList<TNode> list, Direction dir, Func<TNode, bool> filter)
            where TNode : class, IBidirListNode<TNode>
        {
            TNode node = list.GetFirst(dir);
            while (node != null && node != list.GetEnd(dir) && !filter(node))
                node = node.GetNext(dir);
            return node;
        }

        public static TNode GetLast<TNode>(this IBidirList<TNode> list, Func<TNode, bool> filter)
            where TNode : class, IBidirListNode<TNode>
        {
            return GetLast(list, Direction.LeftToRight, filter);
        }

        public static TNode GetLast<TNode>(this IBidirList<TNode> list, Direction dir, Func<TNode, bool> filter)
            where TNode : class, IBidirListNode<TNode>
        {
            TNode node = list.GetLast(dir);
            while (node != null && node != list.GetBegin(dir) && !filter(node))
                node = node.GetPrev(dir);
            return node;
        }

        public static TNode GetNext<TNode>(
            this IBidirList<TNode> list,
            TNode cur,
            Direction dir,
            Func<TNode, TNode, bool> filter
        )
            where TNode : class, IBidirListNode<TNode>
        {
            return cur.GetNext(dir, filter);
        }

        public static TNode GetNext<TNode>(
            this IBidirList<TNode> list,
            TNode cur,
            Direction dir,
            Func<TNode, bool> filter
        )
            where TNode : class, IBidirListNode<TNode>
        {
            return cur.GetNext(dir, filter);
        }

        public static TNode GetPrev<TNode>(
            this IBidirList<TNode> list,
            TNode cur,
            Direction dir,
            Func<TNode, TNode, bool> filter
        )
            where TNode : class, IBidirListNode<TNode>
        {
            return cur.GetPrev(dir, filter);
        }

        public static TNode GetPrev<TNode>(
            this IBidirList<TNode> list,
            TNode cur,
            Direction dir,
            Func<TNode, bool> filter
        )
            where TNode : class, IBidirListNode<TNode>
        {
            return cur.GetPrev(dir, filter);
        }

        public static IEnumerable<TNode> GetNodes<TNode>(this IBidirList<TNode> list, Direction dir)
            where TNode : class, IBidirListNode<TNode>
        {
            if (list.Count == 0)
                return Enumerable.Empty<TNode>();
            return list.GetFirst(dir).GetNodes(dir);
        }

        public static IEnumerable<TNode> GetNodes<TNode>(this IBidirList<TNode> list, TNode first, TNode last)
            where TNode : class, IBidirListNode<TNode>
        {
            return first.GetNodes(last);
        }

        public static IEnumerable<TNode> GetNodes<TNode>(
            this IBidirList<TNode> list,
            TNode first,
            TNode last,
            Direction dir
        )
            where TNode : class, IBidirListNode<TNode>
        {
            return first.GetNodes(last, dir);
        }

        #endregion

        #region IBidirListNode

        public static TNode GetNext<TNode>(this IBidirListNode<TNode> node, Func<TNode, TNode, bool> filter)
            where TNode : class, IBidirListNode<TNode>
        {
            return GetNext(node, Direction.LeftToRight, filter);
        }

        public static TNode GetNext<TNode>(
            this IBidirListNode<TNode> node,
            Direction dir,
            Func<TNode, TNode, bool> filter
        )
            where TNode : class, IBidirListNode<TNode>
        {
            var cur = (TNode)node;
            do
            {
                cur = cur.GetNext(dir);
            } while (cur != node.List.GetEnd(dir) && !filter((TNode)node, cur));
            return cur;
        }

        public static TNode GetNext<TNode>(this IBidirListNode<TNode> node, Func<TNode, bool> filter)
            where TNode : class, IBidirListNode<TNode>
        {
            return GetNext(node, Direction.LeftToRight, filter);
        }

        public static TNode GetNext<TNode>(this IBidirListNode<TNode> node, Direction dir, Func<TNode, bool> filter)
            where TNode : class, IBidirListNode<TNode>
        {
            var cur = (TNode)node;
            do
            {
                cur = cur.GetNext(dir);
            } while (cur != node.List.GetEnd(dir) && !filter(cur));
            return cur;
        }

        public static TNode GetPrev<TNode>(this IBidirListNode<TNode> node, Func<TNode, TNode, bool> filter)
            where TNode : class, IBidirListNode<TNode>
        {
            return GetPrev(node, Direction.LeftToRight, filter);
        }

        public static TNode GetPrev<TNode>(
            this IBidirListNode<TNode> node,
            Direction dir,
            Func<TNode, TNode, bool> filter
        )
            where TNode : class, IBidirListNode<TNode>
        {
            var cur = (TNode)node;
            do
            {
                cur = cur.GetPrev(dir);
            } while (cur != node.List.GetBegin(dir) && !filter((TNode)node, cur));
            return cur;
        }

        public static TNode GetPrev<TNode>(this IBidirListNode<TNode> node, Func<TNode, bool> filter)
            where TNode : class, IBidirListNode<TNode>
        {
            return GetPrev(node, Direction.LeftToRight, filter);
        }

        public static TNode GetPrev<TNode>(this IBidirListNode<TNode> node, Direction dir, Func<TNode, bool> filter)
            where TNode : class, IBidirListNode<TNode>
        {
            var cur = (TNode)node;
            do
            {
                cur = cur.GetPrev(dir);
            } while (cur != node.List.GetBegin(dir) && !filter(cur));
            return cur;
        }

        public static IEnumerable<TNode> GetNodes<TNode>(this IBidirListNode<TNode> first)
            where TNode : class, IBidirListNode<TNode>
        {
            return GetNodes(first, Direction.LeftToRight);
        }

        public static IEnumerable<TNode> GetNodes<TNode>(this IBidirListNode<TNode> first, TNode last)
            where TNode : class, IBidirListNode<TNode>
        {
            return GetNodes(first, last, Direction.LeftToRight);
        }

        public static IEnumerable<TNode> GetNodes<TNode>(this IBidirListNode<TNode> first, Direction dir)
            where TNode : class, IBidirListNode<TNode>
        {
            return GetNodes(first, first.List.GetLast(dir), dir);
        }

        public static IEnumerable<TNode> GetNodes<TNode>(this IBidirListNode<TNode> first, TNode last, Direction dir)
            where TNode : class, IBidirListNode<TNode>
        {
            for (var node = (TNode)first; node != last.GetNext(dir); node = node.GetNext(dir))
                yield return node;
        }

        #endregion

        #region IBidirTreeNode

        public static void PreorderTraverse<TNode>(this IBidirTreeNode<TNode> root, Action<TNode> action)
            where TNode : class, IBidirTreeNode<TNode>
        {
            PreorderTraverse(root, action, Direction.LeftToRight);
        }

        public static void PreorderTraverse<TNode>(this IBidirTreeNode<TNode> root, Action<TNode> action, Direction dir)
            where TNode : class, IBidirTreeNode<TNode>
        {
            DepthFirstTraverseNode(root, action, dir, true);
        }

        public static void PostorderTraverse<TNode>(this IBidirTreeNode<TNode> root, Action<TNode> action)
            where TNode : class, IBidirTreeNode<TNode>
        {
            PostorderTraverse(root, action, Direction.LeftToRight);
        }

        public static void PostorderTraverse<TNode>(
            this IBidirTreeNode<TNode> root,
            Action<TNode> action,
            Direction dir
        )
            where TNode : class, IBidirTreeNode<TNode>
        {
            DepthFirstTraverseNode(root, action, dir, false);
        }

        private static void DepthFirstTraverseNode<TNode>(
            IBidirTreeNode<TNode> node,
            Action<TNode> action,
            Direction dir,
            bool preorder
        )
            where TNode : class, IBidirTreeNode<TNode>
        {
            if (preorder)
                action((TNode)node);
            if (!node.IsLeaf)
            {
                foreach (TNode child in node.Children.GetNodes(dir))
                    DepthFirstTraverseNode(child, action, dir, preorder);
            }
            if (!preorder)
                action((TNode)node);
        }

        public static void LevelOrderTraverse<TNode>(this IBidirTreeNode<TNode> root, Action<TNode> action)
            where TNode : class, IBidirTreeNode<TNode>
        {
            LevelOrderTraverse(root, action, Direction.LeftToRight);
        }

        public static void LevelOrderTraverse<TNode>(
            this IBidirTreeNode<TNode> root,
            Action<TNode> action,
            Direction dir
        )
            where TNode : class, IBidirTreeNode<TNode>
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

        public static IEnumerable<TNode> GetNodesDepthFirst<TNode>(this IBidirTreeNode<TNode> root)
            where TNode : class, IBidirTreeNode<TNode>
        {
            return GetNodesDepthFirst(root, Direction.LeftToRight);
        }

        public static IEnumerable<TNode> GetNodesDepthFirst<TNode>(this IBidirTreeNode<TNode> root, Direction dir)
            where TNode : class, IBidirTreeNode<TNode>
        {
            yield return (TNode)root;

            if (!root.IsLeaf)
            {
                foreach (TNode child in root.Children.GetNodes(dir))
                {
                    foreach (TNode node in child.GetNodesDepthFirst(dir))
                        yield return node;
                }
            }
        }

        public static IEnumerable<TNode> GetNodesBreadthFirst<TNode>(this IBidirTreeNode<TNode> root)
            where TNode : class, IBidirTreeNode<TNode>
        {
            return GetNodesBreadthFirst(root, Direction.LeftToRight);
        }

        public static IEnumerable<TNode> GetNodesBreadthFirst<TNode>(this IBidirTreeNode<TNode> root, Direction dir)
            where TNode : class, IBidirTreeNode<TNode>
        {
            var queue = new Queue<TNode>();
            queue.Enqueue((TNode)root);
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

        public static int DescendantCount<TNode>(this IBidirTreeNode<TNode> node)
            where TNode : class, IBidirTreeNode<TNode>
        {
            return node.Children.Count + node.Children.Sum(child => child.DescendantCount());
        }

        public static TNode GetNextDepthFirst<TNode>(this IBidirTreeNode<TNode> node)
            where TNode : class, IBidirTreeNode<TNode>
        {
            return node.GetNextDepthFirst(Direction.LeftToRight);
        }

        public static TNode GetNextDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Direction dir)
            where TNode : class, IBidirTreeNode<TNode>
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
            } while (parent != null);

            return node.GetNext(dir);
        }

        public static TNode GetNextDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Func<TNode, bool> filter)
            where TNode : class, IBidirTreeNode<TNode>
        {
            return node.GetNextDepthFirst(Direction.LeftToRight, filter);
        }

        public static TNode GetNextDepthFirst<TNode>(
            this IBidirTreeNode<TNode> node,
            Direction dir,
            Func<TNode, bool> filter
        )
            where TNode : class, IBidirTreeNode<TNode>
        {
            var cur = (TNode)node;
            do
            {
                cur = cur.GetNextDepthFirst(dir);
            } while (cur != null && cur != cur.List.GetEnd(dir) && !filter(cur));
            return cur;
        }

        public static TNode GetNextDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Func<TNode, TNode, bool> filter)
            where TNode : class, IBidirTreeNode<TNode>
        {
            return node.GetNextDepthFirst(Direction.LeftToRight, filter);
        }

        public static TNode GetNextDepthFirst<TNode>(
            this IBidirTreeNode<TNode> node,
            Direction dir,
            Func<TNode, TNode, bool> filter
        )
            where TNode : class, IBidirTreeNode<TNode>
        {
            var cur = (TNode)node;
            do
            {
                cur = cur.GetNextDepthFirst(dir);
            } while (cur != null && cur != cur.List.GetEnd(dir) && !filter((TNode)node, cur));
            return cur;
        }

        public static TNode GetPrevDepthFirst<TNode>(this IBidirTreeNode<TNode> node)
            where TNode : class, IBidirTreeNode<TNode>
        {
            return node.GetPrevDepthFirst(Direction.LeftToRight);
        }

        public static TNode GetPrevDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Direction dir)
            where TNode : class, IBidirTreeNode<TNode>
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
            } while (parent != null);

            return node.GetPrev(dir);
        }

        public static TNode GetPrevDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Func<TNode, bool> filter)
            where TNode : class, IBidirTreeNode<TNode>
        {
            return node.GetPrevDepthFirst(Direction.LeftToRight, filter);
        }

        public static TNode GetPrevDepthFirst<TNode>(
            this IBidirTreeNode<TNode> node,
            Direction dir,
            Func<TNode, bool> filter
        )
            where TNode : class, IBidirTreeNode<TNode>
        {
            var cur = (TNode)node;
            do
            {
                cur = cur.GetPrevDepthFirst(dir);
            } while (cur != null && cur != cur.List.GetBegin(dir) && !filter(cur));
            return cur;
        }

        public static TNode GetPrevDepthFirst<TNode>(this IBidirTreeNode<TNode> node, Func<TNode, TNode, bool> filter)
            where TNode : class, IBidirTreeNode<TNode>
        {
            return node.GetPrevDepthFirst(Direction.LeftToRight, filter);
        }

        public static TNode GetPrevDepthFirst<TNode>(
            this IBidirTreeNode<TNode> node,
            Direction dir,
            Func<TNode, TNode, bool> filter
        )
            where TNode : class, IBidirTreeNode<TNode>
        {
            var cur = (TNode)node;
            do
            {
                cur = cur.GetPrevDepthFirst(dir);
            } while (cur != null && cur != cur.List.GetBegin(dir) && !filter((TNode)node, cur));
            return cur;
        }

        #endregion

        public static IEnumerable<T> Items<T>(this IEnumerable<T> source, Direction dir)
        {
            return dir == Direction.LeftToRight ? source : source.Reverse();
        }
    }
}
