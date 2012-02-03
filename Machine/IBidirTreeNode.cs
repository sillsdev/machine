namespace SIL.Machine
{
	public interface IBidirTreeNode<TNode> : IBidirListNode<TNode> where TNode : class, IBidirTreeNode<TNode>
	{
		TNode Parent { get; }

		IBidirList<TNode> Children { get; }
	}
}
