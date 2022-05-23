namespace SIL.Machine.DataStructures
{
    public abstract class PriorityQueueNodeBase
    {
        private int _queueIndex;

        /// <summary>
        /// Represents the current position in the queue
        /// </summary>
        internal int QueueIndex
        {
            get { return _queueIndex; }
            set { _queueIndex = value; }
        }
    }
}
