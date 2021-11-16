using System;
using System.Collections.Generic;
using System.Linq;
using QuickGraph;
using SIL.ObjectModel;

namespace SIL.Machine.Clusterers
{
	public class NeighborJoiningClusterer<T> : IUnrootedHierarchicalClusterer<T>
	{
		private readonly Func<T, T, double> _getDistance;

		public NeighborJoiningClusterer(Func<T, T, double> getDistance)
		{
			_getDistance = getDistance;
		}

		public IUndirectedGraph<Cluster<T>, ClusterEdge<T>> GenerateClusters(IEnumerable<T> dataObjects)
		{
			var tree = new BidirectionalGraph<Cluster<T>, ClusterEdge<T>>(false);
			var clusters = new List<Cluster<T>>();
			foreach (T dataObject in dataObjects)
			{
				var cluster = new Cluster<T>(dataObject) { Description = dataObject.ToString() };
				clusters.Add(cluster);
				tree.AddVertex(cluster);
			}
			var distances = new Dictionary<UnorderedTuple<Cluster<T>, Cluster<T>>, double>();
			for (int i = 0; i < clusters.Count; i++)
			{
				for (int j = i + 1; j < clusters.Count; j++)
				{
					double distance = _getDistance(clusters[i].DataObjects.First(), clusters[j].DataObjects.First());
					if (double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0)
						throw new ArgumentException("Invalid distance between data objects.", "dataObjects");
					distances[UnorderedTuple.Create(clusters[i], clusters[j])] = distance;
				}
			}

			while (clusters.Count > 2)
			{
				Dictionary<Cluster<T>, double> r = clusters.ToDictionary(c => c, c => clusters.Where(oc => oc != c).Sum(oc => distances[UnorderedTuple.Create(c, oc)] / (clusters.Count - 2)));
				int minI = 0, minJ = 0;
				double minDist = 0, minQ = double.MaxValue;
				for (int i = 0; i < clusters.Count; i++)
				{
					for (int j = i + 1; j < clusters.Count; j++)
					{
						double dist = distances[UnorderedTuple.Create(clusters[i], clusters[j])];
						double q = dist - r[clusters[i]] - r[clusters[j]];
						if (q < minQ)
						{
							minQ = q;
							minDist = dist;
							minI = i;
							minJ = j;
						}
					}
				}

				Cluster<T> iCluster = clusters[minI];
				Cluster<T> jCluster = clusters[minJ];
				distances.Remove(UnorderedTuple.Create(iCluster, jCluster));

				var uCluster = new Cluster<T> { Description = "BRANCH" };
				tree.AddVertex(uCluster);

				double iLen = (minDist / 2) + ((r[iCluster] - r[jCluster]) / 2);
				if (iLen <= 0 && !tree.IsOutEdgesEmpty(iCluster))
				{
					foreach (ClusterEdge<T> edge in tree.OutEdges(iCluster))
						tree.AddEdge(new ClusterEdge<T>(uCluster, edge.Target, edge.Length));
					tree.RemoveVertex(iCluster);
				}
				else
				{
					tree.RemoveInEdgeIf(iCluster, edge => true);
					tree.AddEdge(new ClusterEdge<T>(uCluster, iCluster, Math.Max(iLen, 0)));
				}
				double jLen = minDist - iLen;
				if (jLen <= 0 && !tree.IsOutEdgesEmpty(jCluster))
				{
					foreach (ClusterEdge<T> edge in tree.OutEdges(jCluster))
						tree.AddEdge(new ClusterEdge<T>(uCluster, edge.Target, edge.Length));
					tree.RemoveVertex(jCluster);
				}
				else
				{
					tree.RemoveInEdgeIf(jCluster, edge => true);
					tree.AddEdge(new ClusterEdge<T>(uCluster, jCluster, Math.Max(jLen, 0)));
				}

				foreach (Cluster<T> kCluster in clusters.Where(c => c != iCluster && c != jCluster))
				{
					UnorderedTuple<Cluster<T>, Cluster<T>> kiKey = UnorderedTuple.Create(kCluster, iCluster);
					UnorderedTuple<Cluster<T>, Cluster<T>> kjKey = UnorderedTuple.Create(kCluster, jCluster);
					distances[UnorderedTuple.Create(kCluster, uCluster)] = (distances[kiKey] + distances[kjKey] - minDist) / 2;
					distances.Remove(kiKey);
					distances.Remove(kjKey);
				}
				clusters.RemoveAt(minJ);
				clusters.RemoveAt(minI);
				clusters.Add(uCluster);
			}

			if (clusters.Count == 2)
			{
				tree.AddEdge(new ClusterEdge<T>(clusters[1], clusters[0], distances[UnorderedTuple.Create(clusters[0], clusters[1])]));
				clusters.RemoveAt(0);
			}

			var unrootedTree = new UndirectedGraph<Cluster<T>, ClusterEdge<T>>(false);
			unrootedTree.AddVertexRange(tree.Vertices);
			unrootedTree.AddEdgeRange(tree.Edges);
			return unrootedTree;
		}
	}
}
