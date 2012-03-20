namespace SIL.Collections
{
	public interface IOrderedBidirListNode<TNode> : IBidirListNode<TNode> where TNode : class, IBidirListNode<TNode>
	{
		void AddAfter(TNode newNode, Direction dir);

		void AddAfter(TNode newNode);
	}
}
