namespace SIL.Machine.DataStructures
{
	public class PriorityQueueNode<TPriority, TItem> : PriorityQueueNodeBase
	{
		private TPriority _priority;
		private readonly TItem _item;

		public PriorityQueueNode(TPriority priority, TItem item)
		{
			_priority = priority;
			_item = item;
		}

		/// <summary>
		/// The Priority to insert this node at.  Must be set BEFORE adding a node to the queue (ideally just once,
		/// in the node's constructor).
		/// Should not be manually edited once the node has been enqueued - use queue.UpdatePriority() instead.
		/// </summary>
		public TPriority Priority
		{
			get { return _priority; }
			set { _priority = value; }
		}

		public TItem Item
		{
			get { return _item; }
		}
	}
}
