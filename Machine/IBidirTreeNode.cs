namespace SIL.Machine
{
	public interface IBidirTreeNode<TNode> : IBidirListNode<TNode> where TNode : class, IBidirTreeNode<TNode>
	{
		TNode Parent { get; }

		bool IsLeaf { get; }

		int Depth { get; }

		IBidirList<TNode> Children { get; }
	}
}
