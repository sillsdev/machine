using System;
using System.Collections;
using System.Collections.Generic;

namespace SIL.Machine.DataStructures
{
    public class PriorityQueue<TPriority, TItem> : PriorityQueue<PriorityQueueNode<TPriority, TItem>>
    {
        public PriorityQueue()
            : this(10, Comparer<TPriority>.Default) { }

        public PriorityQueue(int capacity)
            : this(capacity, Comparer<TPriority>.Default) { }

        public PriorityQueue(IComparer<TPriority> comparer)
            : base(10, new NodeComparer(comparer)) { }

        public PriorityQueue(int capacity, IComparer<TPriority> comparer)
            : base(capacity, new NodeComparer(comparer)) { }

        public void Enqueue(TPriority priority, TItem item)
        {
            Enqueue(new PriorityQueueNode<TPriority, TItem>(priority, item));
        }

        private class NodeComparer : IComparer<PriorityQueueNode<TPriority, TItem>>
        {
            private readonly IComparer<TPriority> _priorityComparer;

            public NodeComparer(IComparer<TPriority> priorityComparer)
            {
                _priorityComparer = priorityComparer;
            }

            public int Compare(PriorityQueueNode<TPriority, TItem> x, PriorityQueueNode<TPriority, TItem> y)
            {
                return _priorityComparer.Compare(x.Priority, y.Priority);
            }
        }
    }

    /// <summary>
    /// An implementation of a min-Priority Queue using a heap.  Has O(1) .Contains()!
    /// </summary>
    /// <typeparam name="T">The values in the queue.  Must extend the FastPriorityQueueNode class</typeparam>
    public class PriorityQueue<T> : IEnumerable<T>
        where T : PriorityQueueNodeBase
    {
        private int _numNodes;
        private T[] _nodes;
        private readonly IComparer<T> _comparer;
        private int _capacity;

        public PriorityQueue()
            : this(10, Comparer<T>.Default) { }

        public PriorityQueue(int capacity)
            : this(capacity, Comparer<T>.Default) { }

        public PriorityQueue(IComparer<T> comparer)
            : this(10, comparer) { }

        public PriorityQueue(int capacity, IComparer<T> comparer)
        {
            _capacity = capacity;
            _numNodes = 0;
            _nodes = new T[(capacity > 0 ? capacity : 10) + 1];
            _comparer = comparer;
        }

        public bool IsEmpty
        {
            get { return _numNodes == 0; }
        }

        /// <summary>
        /// Returns the number of nodes in the queue.
        /// O(1)
        /// </summary>
        public int Count
        {
            get { return _numNodes; }
        }

        /// <summary>
        /// Returns the total number of items the internal data structure can hold without resizing.
        /// O(1)
        /// </summary>
        public int Capacity
        {
            get { return _capacity; }
            set
            {
#if DEBUG
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "The capacity cannot be less than 0.");

                if (value < _numNodes)
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        "The capacity cannot be less than the current count."
                    );
#endif

                Resize(value);
                _capacity = value;
            }
        }

        /// <summary>
        /// Removes every node from the queue.
        /// O(n) (So, don't do this often!)
        /// </summary>
        public void Clear()
        {
            Array.Clear(_nodes, 1, _numNodes);
            _numNodes = 0;
        }

        /// <summary>
        /// Returns (in O(1)!) whether the given node is in the queue.
        /// O(1)
        /// </summary>
        public bool Contains(T node)
        {
#if DEBUG
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (node.QueueIndex < 0 || node.QueueIndex >= _nodes.Length)
                throw new InvalidOperationException("The queue index is invalid.");
#endif

            return _nodes[node.QueueIndex] == node;
        }

        /// <summary>
        /// Enqueue a node to the priority queue.  Lower values are placed in front. Ties are broken arbitrarily.
        /// If the queue is full, the result is undefined.
        /// If the node is already enqueued, the result is undefined.
        /// O(log n)
        /// </summary>
        public void Enqueue(T node)
        {
#if DEBUG
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (Contains(node))
                throw new ArgumentException("The node is already enqueued.", nameof(node));
#endif
            if (_numNodes == _capacity)
                Capacity = _capacity * 2 + 1;

            _numNodes++;
            _nodes[_numNodes] = node;
            node.QueueIndex = _numNodes;
            CascadeUp(node);
        }

        private void CascadeUp(T node)
        {
            //aka Heapify-up
            int parent;
            if (node.QueueIndex > 1)
            {
                parent = node.QueueIndex >> 1;
                T parentNode = _nodes[parent];
                if (HasHigherOrEqualPriority(parentNode, node))
                    return;

                //Node has lower priority value, so move parent down the heap to make room
                _nodes[node.QueueIndex] = parentNode;
                parentNode.QueueIndex = node.QueueIndex;

                node.QueueIndex = parent;
            }
            else
                return;
            while (parent > 1)
            {
                parent >>= 1;
                T parentNode = _nodes[parent];
                if (HasHigherOrEqualPriority(parentNode, node))
                    break;

                //Node has lower priority value, so move parent down the heap to make room
                _nodes[node.QueueIndex] = parentNode;
                parentNode.QueueIndex = node.QueueIndex;

                node.QueueIndex = parent;
            }
            _nodes[node.QueueIndex] = node;
        }

        private void CascadeDown(T node)
        {
            //aka Heapify-down
            int finalQueueIndex = node.QueueIndex;
            int childLeftIndex = 2 * finalQueueIndex;

            // If leaf node, we're done
            if (childLeftIndex > _numNodes)
                return;

            // Check if the left-child is higher-priority than the current node
            int childRightIndex = childLeftIndex + 1;
            T childLeft = _nodes[childLeftIndex];
            if (HasHigherPriority(childLeft, node))
            {
                // Check if there is a right child. If not, swap and finish.
                if (childRightIndex > _numNodes)
                {
                    node.QueueIndex = childLeftIndex;
                    childLeft.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = childLeft;
                    _nodes[childLeftIndex] = node;
                    return;
                }
                // Check if the left-child is higher-priority than the right-child
                T childRight = _nodes[childRightIndex];
                if (HasHigherPriority(childLeft, childRight))
                {
                    // left is highest, move it up and continue
                    childLeft.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = childLeft;
                    finalQueueIndex = childLeftIndex;
                }
                else
                {
                    // right is even higher, move it up and continue
                    childRight.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = childRight;
                    finalQueueIndex = childRightIndex;
                }
            }
            // Not swapping with left-child, does right-child exist?
            else if (childRightIndex > _numNodes)
                return;
            else
            {
                // Check if the right-child is higher-priority than the current node
                T childRight = _nodes[childRightIndex];
                if (HasHigherPriority(childRight, node))
                {
                    childRight.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = childRight;
                    finalQueueIndex = childRightIndex;
                }
                // Neither child is higher-priority than current, so finish and stop.
                else
                    return;
            }

            while (true)
            {
                childLeftIndex = 2 * finalQueueIndex;

                // If leaf node, we're done
                if (childLeftIndex > _numNodes)
                {
                    node.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = node;
                    break;
                }

                // Check if the left-child is higher-priority than the current node
                childRightIndex = childLeftIndex + 1;
                childLeft = _nodes[childLeftIndex];
                if (HasHigherPriority(childLeft, node))
                {
                    // Check if there is a right child. If not, swap and finish.
                    if (childRightIndex > _numNodes)
                    {
                        node.QueueIndex = childLeftIndex;
                        childLeft.QueueIndex = finalQueueIndex;
                        _nodes[finalQueueIndex] = childLeft;
                        _nodes[childLeftIndex] = node;
                        break;
                    }
                    // Check if the left-child is higher-priority than the right-child
                    T childRight = _nodes[childRightIndex];
                    if (HasHigherPriority(childLeft, childRight))
                    {
                        // left is highest, move it up and continue
                        childLeft.QueueIndex = finalQueueIndex;
                        _nodes[finalQueueIndex] = childLeft;
                        finalQueueIndex = childLeftIndex;
                    }
                    else
                    {
                        // right is even higher, move it up and continue
                        childRight.QueueIndex = finalQueueIndex;
                        _nodes[finalQueueIndex] = childRight;
                        finalQueueIndex = childRightIndex;
                    }
                }
                // Not swapping with left-child, does right-child exist?
                else if (childRightIndex > _numNodes)
                {
                    node.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = node;
                    break;
                }
                else
                {
                    // Check if the right-child is higher-priority than the current node
                    T childRight = _nodes[childRightIndex];
                    if (HasHigherPriority(childRight, node))
                    {
                        childRight.QueueIndex = finalQueueIndex;
                        _nodes[finalQueueIndex] = childRight;
                        finalQueueIndex = childRightIndex;
                    }
                    // Neither child is higher-priority than current, so finish and stop.
                    else
                    {
                        node.QueueIndex = finalQueueIndex;
                        _nodes[finalQueueIndex] = node;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if 'higher' has higher priority than 'lower', false otherwise.
        /// Note that calling HasHigherPriority(node, node) (ie. both arguments the same node) will return false
        /// </summary>
        private bool HasHigherPriority(T higher, T lower)
        {
            return _comparer.Compare(higher, lower) < 0;
        }

        /// <summary>
        /// Returns true if 'higher' has higher priority than 'lower', false otherwise.
        /// Note that calling HasHigherOrEqualPriority(node, node) (ie. both arguments the same node) will return true
        /// </summary>
        private bool HasHigherOrEqualPriority(T higher, T lower)
        {
            return _comparer.Compare(higher, lower) <= 0;
        }

        /// <summary>
        /// Removes the head of the queue and returns it.
        /// If queue is empty, result is undefined
        /// O(log n)
        /// </summary>
        public T Dequeue()
        {
#if DEBUG
            if (_numNodes <= 0)
                throw new InvalidOperationException("The queue is empty.");

            if (!IsValidQueue())
                throw new InvalidOperationException(
                    "Queue has been corrupted (Did you update a node priority manually instead of calling "
                        + "UpdatePriority()? Or add the same node to two different queues?)"
                );
#endif

            T returnMe = _nodes[1];
            //If the node is already the last node, we can remove it immediately
            if (_numNodes == 1)
            {
                _nodes[1] = null;
                _numNodes = 0;
                return returnMe;
            }

            //Swap the node with the last node
            T formerLastNode = _nodes[_numNodes];
            _nodes[1] = formerLastNode;
            formerLastNode.QueueIndex = 1;
            _nodes[_numNodes] = null;
            _numNodes--;

            //Now bubble formerLastNode (which is no longer the last node) down
            CascadeDown(formerLastNode);
            return returnMe;
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the PriorityQueue, if that number is less than a
        /// threshold value.
        /// O(n)
        /// </summary>
        public void TrimExcess()
        {
            if ((double)_numNodes / _capacity > 0.9)
                return;

            Capacity = _numNodes;
        }

        private void Resize(int capacity)
        {
            var newArray = new T[capacity + 1];
            int highestIndexToCopy = Math.Min(capacity, _numNodes);
            Array.Copy(_nodes, newArray, highestIndexToCopy + 1);
            _nodes = newArray;
        }

        /// <summary>
        /// Returns the head of the queue, without removing it (use Dequeue() for that).
        /// If the queue is empty, behavior is undefined.
        /// O(1)
        /// </summary>
        public T Peek()
        {
#if DEBUG
            if (_numNodes <= 0)
                throw new InvalidOperationException("The queue is empty.");
#endif

            return _nodes[1];
        }

        /// <summary>
        /// This method must be called on a node every time its priority changes while it is in the queue.
        /// <b>Forgetting to call this method will result in a corrupted queue!</b>
        /// Calling this method on a node not in the queue results in undefined behavior.
        /// O(log n)
        /// </summary>
        public void UpdatePriority(T node)
        {
#if DEBUG
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (!Contains(node))
                throw new ArgumentException("The node has not been enqueued.", nameof(node));
#endif

            OnNodeUpdated(node);
        }

        private void OnNodeUpdated(T node)
        {
            //Bubble the updated node up or down as appropriate
            int parentIndex = node.QueueIndex >> 1;

            if (parentIndex > 0 && HasHigherPriority(node, _nodes[parentIndex]))
                CascadeUp(node);
            else
                //Note that CascadeDown will be called if parentNode == node (that is, node is the root)
                CascadeDown(node);
        }

        /// <summary>
        /// Removes a node from the queue.  The node does not need to be the head of the queue.
        /// If the node is not in the queue, the result is undefined.  If unsure, check Contains() first
        /// O(log n)
        /// </summary>
        public void Remove(T node)
        {
#if DEBUG
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (!Contains(node))
                throw new ArgumentException("The node has not been enqueued.", nameof(node));
#endif

            //If the node is already the last node, we can remove it immediately
            if (node.QueueIndex == _numNodes)
            {
                _nodes[_numNodes] = null;
                _numNodes--;
                return;
            }

            //Swap the node with the last node
            T formerLastNode = _nodes[_numNodes];
            _nodes[node.QueueIndex] = formerLastNode;
            formerLastNode.QueueIndex = node.QueueIndex;
            _nodes[_numNodes] = null;
            _numNodes--;

            //Now bubble formerLastNode (which is no longer the last node) up or down as appropriate
            OnNodeUpdated(formerLastNode);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 1; i <= _numNodes; i++)
                yield return _nodes[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// <b>Should not be called in production code.</b>
        /// Checks to make sure the queue is still in a valid state.  Used for testing/debugging the queue.
        /// </summary>
        public bool IsValidQueue()
        {
            for (int i = 1; i < _nodes.Length; i++)
            {
                if (_nodes[i] != null)
                {
                    int childLeftIndex = 2 * i;
                    if (
                        childLeftIndex < _nodes.Length
                        && _nodes[childLeftIndex] != null
                        && HasHigherPriority(_nodes[childLeftIndex], _nodes[i])
                    )
                    {
                        return false;
                    }

                    int childRightIndex = childLeftIndex + 1;
                    if (
                        childRightIndex < _nodes.Length
                        && _nodes[childRightIndex] != null
                        && HasHigherPriority(_nodes[childRightIndex], _nodes[i])
                    )
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
