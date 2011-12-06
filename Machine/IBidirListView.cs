namespace SIL.Machine
{
	public interface IBidirListView<TNode> : IBidirList<TNode> where TNode : class, IBidirListNode<TNode>
	{
		IBidirList<TNode> List { get; }

		bool IsValid { get; }

		void SlideNext(int num, Direction dir);

		void SlidePrev(int num, Direction dir);
	}
}
