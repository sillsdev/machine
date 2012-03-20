using System;
using System.Collections.Generic;

namespace SIL.Collections
{
	public abstract class OrderedBidirTreeNode<TNode> : OrderedBidirListNode<TNode>, IOrderedBidirTreeNode<TNode> where TNode : OrderedBidirTreeNode<TNode>
	{
		private readonly Func<bool, TNode> _marginSelector; 
		private OrderedBidirList<TNode> _children;

		protected OrderedBidirTreeNode(Func<bool, TNode> marginSelector)
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

		IBidirList<TNode> IBidirTreeNode<TNode>.Children
		{
			get { return Children; }
		}

		public IOrderedBidirList<TNode> Children
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

		protected internal override void Init(OrderedBidirList<TNode> list)
		{
			base.Init(list);
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

		private class TreeBidirList : OrderedBidirList<TNode>
		{
			private readonly TNode _parent;

			public TreeBidirList(Func<bool, TNode> marginSelector, TNode parent)
				: base(EqualityComparer<TNode>.Default, marginSelector)
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
