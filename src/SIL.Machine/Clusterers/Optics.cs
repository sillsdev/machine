using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.DataStructures;

namespace SIL.Machine.Clusterers
{
    public class Optics<T>
    {
        private readonly Func<T, IEnumerable<Tuple<T, double>>> _getNeighbors;

        public Optics(Func<T, IEnumerable<Tuple<T, double>>> getNeighbors, int minPoints)
        {
            _getNeighbors = getNeighbors;
            MinPoints = minPoints;
        }

        public int MinPoints { get; }

        public IList<ClusterOrderEntry<T>> ClusterOrder(IEnumerable<T> dataObjects)
        {
            var clusterOrder = new List<ClusterOrderEntry<T>>();
            var processed = new HashSet<T>();
            foreach (T dataObject in dataObjects)
            {
                if (!processed.Contains(dataObject))
                    ExpandClusterOrder(clusterOrder, processed, dataObject);
            }
            return clusterOrder;
        }

        private void ExpandClusterOrder(List<ClusterOrderEntry<T>> clusterOrder, HashSet<T> processed, T dataObject)
        {
            var priorityQueue = new PriorityQueue<double, T>();
            var enqueuedNodes = new Dictionary<T, PriorityQueueNode<double, T>>();
            var dataObjectNode = new PriorityQueueNode<double, T>(double.PositiveInfinity, dataObject);
            enqueuedNodes[dataObject] = dataObjectNode;
            priorityQueue.Enqueue(dataObjectNode);
            while (!priorityQueue.IsEmpty)
            {
                PriorityQueueNode<double, T> node = priorityQueue.Dequeue();
                enqueuedNodes.Remove(node.Item);
                double reachability = node.Priority;
                T current = node.Item;
                processed.Add(current);

                List<Tuple<T, double>> neighbors = _getNeighbors(current).OrderBy(n => n.Item2).ToList();
                double coreDistance = double.PositiveInfinity;
                if (neighbors.Count >= MinPoints)
                {
                    coreDistance = neighbors[MinPoints - 1].Item2;
                    foreach (Tuple<T, double> neighbor in neighbors)
                    {
                        if (!processed.Contains(neighbor.Item1))
                        {
                            double priority = Math.Max(neighbor.Item2, coreDistance);
                            if (
                                enqueuedNodes.TryGetValue(neighbor.Item1, out PriorityQueueNode<double, T> neighborNode)
                            )
                            {
                                neighborNode.Priority = priority;
                                priorityQueue.UpdatePriority(neighborNode);
                            }
                            else
                            {
                                neighborNode = new PriorityQueueNode<double, T>(priority, neighbor.Item1);
                                enqueuedNodes[neighbor.Item1] = neighborNode;
                                priorityQueue.Enqueue(neighborNode);
                            }
                        }
                    }
                }

                clusterOrder.Add(new ClusterOrderEntry<T>(current, reachability, coreDistance));
            }
        }
    }
}
