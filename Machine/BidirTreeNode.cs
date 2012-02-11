using System;

namespace SIL.Machine
{
	public class BidirTreeNode<TNode> : BidirListNode<TNode>, IBidirTreeNode<TNode> where TNode : BidirTreeNode<TNode>
	{
		private readonly BidirList<TNode> _children;

		public BidirTreeNode()
		{
			_children = new TreeBidirList((TNode) this);
			Depth = -1;
		}

		public TNode Parent { get; private set; }

		public int Depth { get; private set; }

		public IBidirList<TNode> Children
		{
			get { return _children; }
		}

		protected internal override void Clear()
		{
			base.Clear();
			Parent = null;
			Depth = -1;
		}

		protected internal override void Init(BidirList<TNode> list, bool singleState)
		{
			base.Init(list, singleState);
			Parent = ((TreeBidirList) list).Parent;
			Depth = Parent == null ? 0 : Parent.Depth + 1;
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

			public override void Add(TNode node)
			{
				if (!_parent.CanAdd(node))
					throw new ArgumentException("The specified node cannot be added to this node.", "node");
				base.Add(node);
			}
		}
	}
}
