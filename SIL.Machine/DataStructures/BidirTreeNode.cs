using System;
using System.Collections.Generic;

namespace SIL.Machine.DataStructures
{
	public abstract class BidirTreeNode<TNode> : BidirListNode<TNode>, IBidirTreeNode<TNode> where TNode : BidirTreeNode<TNode>
	{
		private readonly Func<bool, TNode> _marginSelector; 
		private BidirList<TNode> _children;

		protected BidirTreeNode(Func<bool, TNode> marginSelector)
		{
			_marginSelector = marginSelector;
			Root = (TNode) this;
		}

		public TNode Parent { get; private set; }

		public int Depth { get; private set; }

		public bool IsLeaf
		{
			get { return _children == null || _children.Count == 0; }
		}

		public TNode Root { get; private set; }

		public IBidirList<TNode> Children
		{
			get
			{
				if (_children == null)
					_children = new TreeBidirList(_marginSelector, (TNode) this);
				return _children;
			}
		}

		protected internal override void Clear()
		{
			base.Clear();
			Parent = null;
			Depth = 0;
			Root = (TNode) this;
		}

		protected internal override void Init(BidirList<TNode> list, int levels)
		{
			base.Init(list, levels);
			Parent = ((TreeBidirList) list).Parent;
			if (Parent != null)
			{
				Depth = Parent.Depth + 1;
				Root = Parent.Root;
			}
		}

		protected virtual bool CanAdd(TNode child)
		{
			return true;
		}

		private class TreeBidirList : BidirList<TNode>
		{
			private readonly TNode _parent;

			public TreeBidirList(Func<bool, TNode> marginSelector, TNode parent)
				: base(Comparer<TNode>.Default, marginSelector)
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
