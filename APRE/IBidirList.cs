using System.Collections.Generic;

namespace SIL.APRE
{
	public enum Direction { LeftToRight, RightToLeft };

	public interface IBidirList<TNode> : ICollection<TNode> where TNode : class, IBidirListNode<TNode>
	{
		TNode Begin { get; }

		TNode End { get; }

		TNode GetBegin(Direction dir);

		TNode GetEnd(Direction dir);

		/// <summary>
		/// Gets the first node in this list.
		/// </summary>
		/// <value>The first node.</value>
		TNode First { get; }

		/// <summary>
		/// Gets the last node in this list.
		/// </summary>
		/// <value>The last node.</value>
		TNode Last { get; }

		/// <summary>
		/// Gets the first node in this list according to the specified direction.
		/// </summary>
		/// <param name="dir">The direction.</param>
		/// <returns>The first node.</returns>
		TNode GetFirst(Direction dir);

		/// <summary>
		/// Gets the last node in this list according to the specified direction.
		/// </summary>
		/// <param name="dir">The direction.</param>
		/// <returns>The last node.</returns>
		TNode GetLast(Direction dir);

		/// <summary>
		/// Gets the node after the specified node.
		/// </summary>
		/// <param name="cur">The current node.</param>
		/// <returns>The next node.</returns>
		TNode GetNext(TNode cur);

		/// <summary>
		/// Gets the node after the specified node according to the specified direction.
		/// </summary>
		/// <param name="cur">The current node.</param>
		/// <param name="dir">The direction.</param>
		/// <returns>The next node.</returns>
		/// <exception cref="System.ArgumentException">Thrown when the specified node is not owned by this linked list.</exception>
		TNode GetNext(TNode cur, Direction dir);

		/// <summary>
		/// Gets the node before the specified node.
		/// </summary>
		/// <param name="cur">The current node.</param>
		/// <returns>The previous node.</returns>
		TNode GetPrev(TNode cur);

		/// <summary>
		/// Gets the node before the specified node according to the specified direction.
		/// </summary>
		/// <param name="cur">The current node.</param>
		/// <param name="dir">The direction.</param>
		/// <returns>The previous node.</returns>
		/// <exception cref="System.ArgumentException">Thrown when the specified node is not owned by this linked list.</exception>
		TNode GetPrev(TNode cur, Direction dir);

		/// <summary>
		/// Finds the node that matches the specified example node.
		/// </summary>
		/// <param name="example">The example node.</param>
		/// <param name="result">The result.</param>
		/// <returns></returns>
		bool Find(TNode example, out TNode result);

		/// <summary>
		/// Finds the node that matches the specified example node.
		/// </summary>
		/// <param name="start">The node to start searching from.</param>
		/// <param name="example">The example node.</param>
		/// <param name="result">The result.</param>
		/// <returns></returns>
		bool Find(TNode start, TNode example, out TNode result);

		/// <summary>
		/// Finds the node that matches the specified example node.
		/// </summary>
		/// <param name="example">The example node.</param>
		/// <param name="dir">The direction.</param>
		/// <param name="result">The result.</param>
		/// <returns></returns>
		bool Find(TNode example, Direction dir, out TNode result);

		/// <summary>
		/// Finds the node that matches the specified example node.
		/// </summary>
		/// <param name="start">The node to start searching from.</param>
		/// <param name="example">The example node.</param>
		/// <param name="dir">The direction.</param>
		/// <param name="result">The result.</param>
		/// <returns></returns>
		bool Find(TNode start, TNode example, Direction dir, out TNode result);

		IBidirListView<TNode> GetView(TNode first);

		IBidirListView<TNode> GetView(TNode first, TNode last);

		IBidirListView<TNode> GetView(TNode first, Direction dir);

		IBidirListView<TNode> GetView(TNode first, TNode last, Direction dir);
	}
}
