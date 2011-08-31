using System;
using System.Collections.Generic;

namespace SIL.APRE
{
	public class SortedBidirListView<TNode> : BidirListViewBase<TNode> where TNode : class, IBidirListNode<TNode>
	{
		private readonly IComparer<TNode> _leftToRightComparer;

		public SortedBidirListView(TNode first, TNode last, IComparer<TNode> leftToRightComparer)
			: base(first, last)
		{
			_leftToRightComparer = leftToRightComparer;
		}

		public override bool Contains(TNode item)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			if (item.List != List)
				return false;

			return _leftToRightComparer.Compare(item, Last) >= 0 && _leftToRightComparer.Compare(item, Last) <= 0;
		}

		public override bool Find(TNode start, TNode example, Direction dir, out TNode result)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			bool exact = List.Find(start, example, dir, out result);
			if (Contains(result))
				return exact;
			result = null;
			return false;
		}
	}
}
