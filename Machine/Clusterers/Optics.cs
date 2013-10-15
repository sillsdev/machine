using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Machine.Clusterers
{
	public class Optics<T>
	{
		private readonly Func<T, IEnumerable<Tuple<T, double>>> _getNeighbors;
		private readonly int _minPoints;

		public Optics(Func<T, IEnumerable<Tuple<T, double>>> getNeighbors, int minPoints)
		{
			_getNeighbors = getNeighbors;
			_minPoints = minPoints;
		}

		public int MinPoints
		{
			get { return _minPoints; }
		}

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
			priorityQueue.Enqueue(double.PositiveInfinity, dataObject);
			while (!priorityQueue.IsEmpty)
			{
				double reachability;
				T current = priorityQueue.Dequeue(out reachability);
				processed.Add(current);

				List<Tuple<T, double>> neighbors = _getNeighbors(current).OrderBy(n => n.Item2).ToList();
				double coreDistance = double.PositiveInfinity;
				if (neighbors.Count >= _minPoints)
				{
					coreDistance = neighbors[_minPoints - 1].Item2;
					foreach (Tuple<T, double> neighbor in neighbors)
					{
						if (!processed.Contains(neighbor.Item1))
							priorityQueue.Enqueue(Math.Max(neighbor.Item2, coreDistance), neighbor.Item1);
					}
				}

				clusterOrder.Add(new ClusterOrderEntry<T>(current, reachability, coreDistance));
			}
		}
	}
}
