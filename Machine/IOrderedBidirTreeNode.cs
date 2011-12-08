namespace SIL.Machine
{
	public interface IOrderedBidirTreeNode<TNode> : IOrderedBidirListNode<TNode> where TNode : class, IOrderedBidirTreeNode<TNode>
	{
		TNode Parent { get; }
		
		bool IsLeaf { get; }

		IOrderedBidirList<TNode> Children { get; }
	}
}
