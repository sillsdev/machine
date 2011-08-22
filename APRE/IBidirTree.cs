namespace SIL.APRE
{
	public interface IBidirTree<TNode> where TNode : class, IBidirTreeNode<TNode>
	{
		TNode Root { get; }
	}
}
