using System;
using System.Collections.Generic;

namespace SIL.APRE
{
	public class BidirListView<TNode> : BidirListViewBase<TNode> where TNode : class, IBidirListNode<TNode>
	{
		private readonly IEqualityComparer<TNode> _comparer;

		public BidirListView(TNode first, TNode last, IEqualityComparer<TNode> comparer)
			: base(first, last)
		{
			_comparer = comparer;
		}

		public override bool Find(TNode start, TNode example, Direction dir, out TNode result)
		{
			if (!IsValid)
				throw new InvalidOperationException("This view has been invalidated by a modification to its backing list.");

			for (TNode n = start; n != GetLast(dir).GetNext(dir); n = n.GetNext(dir))
			{
				if (_comparer.Equals(example, n))
				{
					result = n;
					return true;
				}
			}
			result = null;
			return false;
		}
	}
}
