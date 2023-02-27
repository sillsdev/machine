namespace SIL.Machine.DataStructures
{
    public interface IBidirTreeNode<TNode> : IBidirListNode<TNode>
        where TNode : class, IBidirTreeNode<TNode>
    {
        TNode Parent { get; }

        int Depth { get; }

        bool IsLeaf { get; }

        TNode Root { get; }

        IBidirList<TNode> Children { get; }
    }
}
