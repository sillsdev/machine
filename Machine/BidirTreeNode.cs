using System;

namespace SIL.Machine
{
	public class BidirTreeNode<TNode> : BidirListNode<TNode>, IBidirTreeNode<TNode> where TNode : BidirTreeNode<TNode>
	{
		private readonly BidirList<TNode> _children;

		public BidirTreeNode()
		{
			_children = new TreeBidirList((TNode) this);
		}

		public TNode Parent { get; private set; }

		public bool IsLeaf
		{
			get { return _children.Count == 0; }
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

		protected internal override void Init(BidirList<TNode> list)
		{
			base.Init(list);
			TNode parent = ((TreeBidirList) list).Parent;
			Parent = parent;
		}

		protected virtual bool CanAdd(TNode child)
		{
			return true;
		}

		private class TreeBidirList : BidirList<TNode>
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

			public override void Insert(TNode node, TNode newNode, Direction dir)
			{
				if (!_parent.CanAdd(newNode))
					throw new ArgumentException("The specified node cannot be added to this node.", "newNode");
				base.Insert(node, newNode, dir);
			}
		}
	}
}
