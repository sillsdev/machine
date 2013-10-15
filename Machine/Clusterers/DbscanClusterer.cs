using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Clusterers
{
	public class DbscanClusterer<T> : IFlatClusterer<T>
	{
		private readonly Func<T, IEnumerable<T>> _getNeighbors;
		private readonly double _minPoints;

		public DbscanClusterer(Func<T, IEnumerable<T>> getNeighbors, double minPoints)
		{
			_getNeighbors = getNeighbors;
			_minPoints = minPoints;
		}

		public IEnumerable<Cluster<T>> GenerateClusters(IEnumerable<T> dataObjects)
		{
			var clusters = new List<Cluster<T>>();
			HashSet<T> currentCluster = null;
			var processed = new HashSet<T>();
			var noise = new HashSet<T>();
			foreach (T dataObject in dataObjects)
			{
				if (!processed.Contains(dataObject))
				{
					if (currentCluster == null)
						currentCluster = new HashSet<T>();

					if (ExpandCluster(processed, noise, currentCluster, dataObject))
					{
						clusters.Add(new Cluster<T>(currentCluster));
						currentCluster = null;
					}
				}
			}
			clusters.Add(new Cluster<T>(noise, true));

			return clusters;
		}

		private bool ExpandCluster(HashSet<T> processed, HashSet<T> noise, HashSet<T> currentCluster, T dataObject)
		{
			var seeds = new HashSet<T>(_getNeighbors(dataObject));
			if (seeds.Count < _minPoints)
			{
				noise.Add(dataObject);
				processed.Add(dataObject);
				return false;
			}

			foreach (T seed in seeds)
			{
				if (!processed.Contains(seed))
				{
					currentCluster.Add(seed);
					processed.Add(seed);
				}
				else if (noise.Contains(seed))
				{
					currentCluster.Add(seed);
					noise.Remove(seed);
				}
			}

			seeds.Remove(dataObject);
			while (seeds.Count > 0)
			{
				T curDataObject = seeds.First();
				var neighborhood = new HashSet<T>(_getNeighbors(curDataObject));
				if (neighborhood.Count >= _minPoints)
				{
					foreach (T p in neighborhood)
					{
						bool inNoise = noise.Contains(p);
						bool unclassified = !processed.Contains(p);
						if (inNoise || unclassified)
						{
							if (unclassified)
								seeds.Add(p);
							currentCluster.Add(p);
							processed.Add(p);
							if (inNoise)
								noise.Remove(p);
						}
					}
				}
				seeds.Remove(curDataObject);
			}

			if (currentCluster.Count < _minPoints)
			{
				noise.UnionWith(currentCluster);
				noise.Add(dataObject);
				processed.Add(dataObject);
				currentCluster.Clear();
				return false;
			}

			return true;
		}
	}
}
