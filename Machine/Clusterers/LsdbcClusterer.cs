using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Clusterers
{
	public class LsdbcClusterer<T> : IFlatClusterer<T>
	{
		private readonly double _factor;
		private readonly Func<T, IEnumerable<Tuple<T, double>>> _getKNearestNeighbors;

		public LsdbcClusterer(double alpha, Func<T, IEnumerable<Tuple<T, double>>> getKNearestNeighbors)
		{
			_factor = Math.Pow(2, alpha);
			_getKNearestNeighbors = getKNearestNeighbors;
		}

		public IEnumerable<Cluster<T>> GenerateClusters(IEnumerable<T> dataObjects)
		{
			var unclassified = new HashSet<T>(dataObjects);
			HashSet<T> currentCluster = null;
			var clusters = new List<Cluster<T>>();
			Dictionary<T, Tuple<double, List<T>>> pointInfos = unclassified.ToDictionary(point => point, point =>
			{
				double epsilon = 0.0;
				var neighbors = new List<T>();
				foreach (Tuple<T, double> neighborInfo in _getKNearestNeighbors(point))
				{
					neighbors.Add(neighborInfo.Item1);
					epsilon = neighborInfo.Item2;
				}
				return Tuple.Create(epsilon, neighbors);
			});
			foreach (KeyValuePair<T, Tuple<double, List<T>>> pointPair in pointInfos.OrderBy(pi => pi.Value.Item1))
			{
				if (unclassified.Contains(pointPair.Key) && pointPair.Value.Item2.All(neighbor => pointPair.Value.Item1 <= pointInfos[neighbor].Item1))
				{
					if (currentCluster != null)
						clusters.Add(new Cluster<T>(currentCluster));
					currentCluster = new HashSet<T>();
					ExpandCluster(unclassified, pointInfos, currentCluster, pointPair.Key, pointPair.Value);
					if (unclassified.Count == 0)
						break;
				}
			}
			clusters.Add(new Cluster<T>(currentCluster));
			clusters.Add(new Cluster<T>(unclassified, true));

			return clusters;
		}

		private void ExpandCluster(HashSet<T> unclassified, Dictionary<T, Tuple<double, List<T>>> pointInfos, HashSet<T> currentCluster, T point,
			Tuple<double, List<T>> pointInfo)
		{
			currentCluster.Add(point);
			unclassified.Remove(point);
			var seeds = new HashSet<T>();
			foreach (T neighbor in pointInfo.Item2)
			{
				if (unclassified.Contains(neighbor))
				{
					currentCluster.Add(neighbor);
					seeds.Add(neighbor);
					unclassified.Remove(neighbor);
				}
			}

			while (seeds.Count > 0)
			{
				T curPoint = seeds.First();
				Tuple<double, List<T>> curPointInfo = pointInfos[curPoint];
				if (curPointInfo.Item1 <= _factor * pointInfo.Item1)
				{
					foreach (T neighbor in curPointInfo.Item2)
					{
						if (unclassified.Contains(neighbor))
						{
							currentCluster.Add(neighbor);
							seeds.Add(neighbor);
							unclassified.Remove(neighbor);
						}
					}
				}
				seeds.Remove(curPoint);
			}
		}
	}
}
