namespace SIL.Machine.DataStructures
{
    public interface IOrderedBidirList<TNode> : IBidirList<TNode>
        where TNode : class, IBidirListNode<TNode>
    {
        void AddAfter(TNode newNode, TNode node, Direction dir);

        void AddAfter(TNode newNode, TNode node);
    }
}
