namespace SIL.Machine
{
	public interface IOrderedBidirListNode<TNode> : IBidirListNode<TNode> where TNode : class, IBidirListNode<TNode>
	{
		void Insert(TNode newNode, Direction dir);

		void Insert(TNode newNode);
	}
}
