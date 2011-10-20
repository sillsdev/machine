namespace SIL.APRE
{
	public interface IBidirTreeNode<TNode> : IOrderedBidirListNode<TNode> where TNode : class, IBidirTreeNode<TNode>
	{
		TNode Parent { get; }
		
		bool IsLeaf { get; }

		IOrderedBidirList<TNode> Children { get; }
	}
}
