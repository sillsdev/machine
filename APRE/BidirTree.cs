namespace SIL.APRE
{
	public class BidirTree<TNode> : IBidirTree<TNode> where TNode : BidirTreeNode<TNode>
	{
		private readonly TNode _root;

		public BidirTree(TNode root)
		{
			_root = root;
			_root.Tree = this;
		}

		public TNode Root
		{
			get { return _root; }
		}
	}
}
