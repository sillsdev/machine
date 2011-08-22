namespace SIL.APRE
{
	public interface IBidirTreeNode<TNode> : IBidirListNode<TNode> where TNode : class, IBidirTreeNode<TNode>
	{
		TNode Parent { get; }
		
		bool IsLeaf { get; }

		IBidirList<TNode> Children { get; }

		IBidirTree<TNode> Tree { get; } 
	}
}
