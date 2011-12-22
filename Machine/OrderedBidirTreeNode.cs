using System;

namespace SIL.Machine
{
	public class OrderedBidirTreeNode<TNode> : OrderedBidirListNode<TNode>, IOrderedBidirTreeNode<TNode> where TNode : OrderedBidirTreeNode<TNode>
	{
		private readonly OrderedBidirList<TNode> _children;

		public OrderedBidirTreeNode()
		{
			_children = new TreeBidirList((TNode) this);
		}

		public TNode Parent { get; private set; }

		public bool IsLeaf
		{
			get { return _children.Count == 0; }
		}

		public int Depth
		{
			get { return Parent == null ? 0 : Parent.Depth + 1; }
		}

		IBidirList<TNode> IBidirTreeNode<TNode>.Children
		{
			get { return Children; }
		}

		public IOrderedBidirList<TNode> Children
		{
			get { return _children; }
		}

		protected internal override void Clear()
		{
			base.Clear();
			Parent = null;
		}

		protected internal override void Init(OrderedBidirList<TNode> list)
		{
			base.Init(list);
			TNode parent = ((TreeBidirList) list).Parent;
			Parent = parent;
		}

		protected virtual bool CanAdd(TNode child)
		{
			return true;
		}

		private class TreeBidirList : OrderedBidirList<TNode>
		{
			private readonly TNode _parent;

			public TreeBidirList(TNode parent)
			{
				_parent = parent;
			}

			public TNode Parent
			{
				get { return _parent; }
			}

			public override void AddAfter(TNode node, TNode newNode, Direction dir)
			{
				if (!_parent.CanAdd(newNode))
					throw new ArgumentException("The specified node cannot be added to this node.", "newNode");
				base.AddAfter(node, newNode, dir);
			}
		}
	}
}
