namespace SIL.APRE
{
	public interface IOrderedBidirList<TNode> : IBidirList<TNode> where TNode : class, IBidirListNode<TNode>
	{
		void Insert(TNode newNode, TNode node, Direction dir);

		void Insert(TNode newNode, TNode node);
	}
}
