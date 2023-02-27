namespace SIL.Machine.DataStructures
{
    public interface IBidirListNode<TNode>
        where TNode : class, IBidirListNode<TNode>
    {
        IBidirList<TNode> List { get; }

        /// <summary>
        /// Gets the next node in the owning linked list.
        /// </summary>
        /// <value>The next node.</value>
        TNode Next { get; }

        /// <summary>
        /// Gets the previous node in the owning linked list.
        /// </summary>
        /// <value>The previous node.</value>
        TNode Prev { get; }

        /// <summary>
        /// Gets the next node in the owning linked list according to the
        /// specified direction.
        /// </summary>
        /// <param name="dir">The direction</param>
        /// <returns>The next node.</returns>
        TNode GetNext(Direction dir);

        /// <summary>
        /// Gets the previous node in the owning linked list according to the
        /// specified direction.
        /// </summary>
        /// <param name="dir">The direction</param>
        /// <returns>The previous node.</returns>
        TNode GetPrev(Direction dir);

        /// <summary>
        /// Removes this node from the owning linked list.
        /// </summary>
        /// <returns><c>true</c> if the node is a member of a linked list, otherwise <c>false</c></returns>
        bool Remove();
    }
}
